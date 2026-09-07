using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Blobs.Application;
using Blobs.Content;
using Blobs.Core;
using Blobs.Input;
using Blobs.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Blobs.Tests.PlayMode
{
    /// <summary>
    /// Covers presentation behavior that depends on Unity frames, object destruction, cloning,
    /// and MonoBehaviour startup and therefore cannot be proven by the EditMode suite.
    /// </summary>
    public sealed class PresentationRuntimeTests
    {
        private readonly List<Object> _createdObjects = new();

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            for (int i = _createdObjects.Count - 1; i >= 0; i--)
            {
                if (_createdObjects[i] != null)
                    Object.Destroy(_createdObjects[i]);
            }

            _createdObjects.Clear();
            yield return null;
        }

        [UnityTest]
        public IEnumerator MoveStepBeatsRunSequentially()
        {
            BoardPresenter presenter = CreatePresenter(EmptySnapshot());
            var events = new List<string>();
            presenter.RegisterMoveStepHandler(new RecordingStepHandler(events));

            presenter.ApplySteps(
                new[]
                {
                    new MoveStep(MoveStepKind.Traverse, Array.Empty<IBoardEffect>()),
                    new MoveStep(MoveStepKind.Traverse, Array.Empty<IBoardEffect>())
                },
                EmptySnapshot());

            yield return WaitUntil(() => events.Count == 4);

            CollectionAssert.AreEqual(
                new[] { "start-1", "end-1", "start-2", "end-2" },
                events);
        }

        [UnityTest]
        public IEnumerator RebuildDuringMergeCleansUpInterruptedViewsAndAnimationState()
        {
            BlobState source = Blob("source", BlobColor.Red, 0, 0);
            BlobState target = Blob("target", BlobColor.Blue, 1, 0);
            GameSessionSnapshot initial = Snapshot(2, 1, source, target);
            BoardPresenter presenter = CreatePresenter(initial);
            Assert.That(presenter.TryGetBlobView(source.Id, out BlobView oldSource), Is.True);
            Assert.That(presenter.TryGetBlobView(target.Id, out BlobView oldTarget), Is.True);

            presenter.ApplySteps(
                new[]
                {
                    new MoveStep(
                        MoveStepKind.Merge,
                        new IBoardEffect[]
                        {
                            new RemoveBlobEffect(target),
                            new MoveBlobEffect(source.Id, source.Position, target.Position)
                        })
                },
                Snapshot(2, 1, source.WithPosition(target.Position)));

            yield return new WaitForSeconds(0.03f);
            Assert.That(oldSource.BlobMotionAnimator.CurrentState, Is.EqualTo(BlobAnimationState.Merging));

            presenter.Rebuild(initial);
            yield return null;

            Assert.That(oldSource == null, Is.True);
            Assert.That(oldTarget == null, Is.True);
            Assert.That(presenter.TryGetBlobView(source.Id, out BlobView restoredSource), Is.True);
            Assert.That(presenter.TryGetBlobView(target.Id, out BlobView restoredTarget), Is.True);
            Assert.That(restoredSource.GridPosition, Is.EqualTo(source.Position));
            Assert.That(restoredTarget.GridPosition, Is.EqualTo(target.Position));
            Assert.That(
                restoredSource.BlobMotionAnimator.CurrentState,
                Is.EqualTo(BlobAnimationState.Idle));
            Assert.That(presenter.IsSynchronizedWith(initial), Is.True);
        }

        [UnityTest]
        public IEnumerator InstantiatedBlobCompositionInvokesEveryContactFeedbackChannel()
        {
            GameObject prefab = CreateGameObject("Composed Blob Prefab", active: false);
            prefab.AddComponent<BlobView>();
            RecordingContactFeedback originalFirst =
                prefab.AddComponent<RecordingContactFeedback>();
            RecordingContactFeedback originalSecond =
                prefab.AddComponent<RecordingContactFeedback>();

            GameObject instance = Object.Instantiate(prefab);
            _createdObjects.Add(instance);
            instance.name = "Composed Blob Instance";
            instance.SetActive(true);
            BlobView view = instance.GetComponent<BlobView>();
            view.Initialize(
                Blob("target", BlobColor.Green, 0, 0),
                CreatePalette(),
                1f,
                Vector2.zero);

            view.PlayContactFeedback(source: null);
            yield return null;

            RecordingContactFeedback[] feedback =
                instance.GetComponents<RecordingContactFeedback>();
            Assert.That(feedback, Has.Length.EqualTo(2));
            Assert.That(feedback[0].InvocationCount, Is.EqualTo(1));
            Assert.That(feedback[1].InvocationCount, Is.EqualTo(1));
            Assert.That(feedback[0].LastContext.Target, Is.SameAs(view));
            Assert.That(feedback[1].LastContext.Target, Is.SameAs(view));
            Assert.That(originalFirst.InvocationCount, Is.Zero);
            Assert.That(originalSecond.InvocationCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator ComposedSceneRootStartsSessionOnFirstFrame()
        {
            GameObject root = CreateGameObject("Runtime Scene Composition", active: false);
            root.AddComponent<MergeAnimationOrchestrator>();
            var surface = new GameObject("Board Surface");
            surface.transform.SetParent(root.transform, false);
            surface.AddComponent<BoardSurfaceView>();

            BoardPresenter board = root.AddComponent<BoardPresenter>();
            GameplayInputAdapter input = root.AddComponent<GameplayInputAdapter>();
            GameplayCommandAdapter commands = root.AddComponent<GameplayCommandAdapter>();
            GameBootstrapper bootstrapper = root.AddComponent<GameBootstrapper>();
            LevelColorPaletteAsset palette = CreatePalette();
            LevelDefinitionAsset level = CreateEmptyLevel(palette);
            BlobViewCatalogAsset blobCatalog = CreateAsset<BlobViewCatalogAsset>();

            SetPrivateField(board.GetComponent<BlobPresenter>(), "_blobViewCatalog", blobCatalog);
            SetPrivateField(bootstrapper, "boardPresenter", board);
            SetPrivateField(bootstrapper, "levelAsset", level);
            SetPrivateField(bootstrapper, "inputAdapter", input);
            SetPrivateField(bootstrapper, "commandAdapter", commands);

            int sessionStartedCount = 0;
            bootstrapper.SessionStarted += (_, _) => sessionStartedCount++;
            root.SetActive(true);

            yield return WaitUntil(() => bootstrapper.CurrentState != null);

            Assert.That(sessionStartedCount, Is.EqualTo(1));
            Assert.That(bootstrapper.CurrentState.CreateSnapshot().LevelId, Is.EqualTo("playmode-startup"));
            Assert.That(board.CurrentSnapshot, Is.Not.Null);
            Assert.That(board.VisibleBlobCount, Is.Zero);
            Assert.That(board.VisibleTileCount, Is.Zero);
            Assert.That(board.VisibleSurfaceCellCount, Is.Zero);
            Assert.That(
                input.SelectBlobAt(new GridPosition(0, 0)).HasSelection,
                Is.False);
        }

        [UnityTest]
        public IEnumerator CameraFramingUsesLiveAspectAndPreservesDepth()
        {
            GameObject cameraObject = CreateGameObject("Presentation Camera");
            cameraObject.transform.position = new Vector3(10f, 20f, -10f);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.aspect = 0.5f;
            CameraPresenter presenter = cameraObject.AddComponent<CameraPresenter>();
            SetPrivateField(presenter, "padding", 0.5f);

            presenter.FitCameraToBoard(width: 5, height: 3, cellSize: 1.25f);
            yield return null;

            Assert.That(cameraObject.transform.position.x, Is.EqualTo(2.5f).Within(0.001f));
            Assert.That(cameraObject.transform.position.y, Is.EqualTo(1.25f).Within(0.001f));
            Assert.That(cameraObject.transform.position.z, Is.EqualTo(-10f).Within(0.001f));
            Assert.That(camera.orthographicSize, Is.EqualTo(6f).Within(0.001f));
        }

        private BoardPresenter CreatePresenter(GameSessionSnapshot initialSnapshot)
        {
            GameObject root = CreateGameObject("PlayMode Board Presenter", active: false);
            root.AddComponent<MergeAnimationOrchestrator>();
            var surface = new GameObject("Board Surface");
            surface.transform.SetParent(root.transform, false);
            surface.AddComponent<BoardSurfaceView>();
            BoardPresenter presenter = root.AddComponent<BoardPresenter>();
            LevelColorPaletteAsset palette = CreatePalette();
            var factory = new RuntimeBlobViewFactory(palette);

            root.SetActive(true);
            presenter.Initialize(
                new FakeGameplayState(initialSnapshot),
                palette,
                factory);
            return presenter;
        }

        private LevelDefinitionAsset CreateEmptyLevel(LevelColorPaletteAsset palette)
        {
            LevelDefinitionAsset level = CreateAsset<LevelDefinitionAsset>();
            SetPrivateField(level, "levelId", "playmode-startup");
            SetPrivateField(level, "schemaVersion", LevelDefinition.CurrentSchemaVersion);
            SetPrivateField(level, "width", 1);
            SetPrivateField(level, "height", 1);
            SetPrivateField(level, "emptyPositions", new List<Vector2Int> { Vector2Int.zero });
            SetPrivateField(level, "blobs", new List<BlobAssetData>());
            SetPrivateField(level, "tiles", new List<TileAssetData>());
            SetPrivateField(level, "palette", palette);
            return level;
        }

        private LevelColorPaletteAsset CreatePalette()
        {
            return CreateAsset<LevelColorPaletteAsset>();
        }

        private T CreateAsset<T>() where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            _createdObjects.Add(asset);
            return asset;
        }

        private GameObject CreateGameObject(string name, bool active = true)
        {
            var gameObject = new GameObject(name);
            gameObject.SetActive(active);
            _createdObjects.Add(gameObject);
            return gameObject;
        }

        private static IEnumerator WaitUntil(Func<bool> predicate)
        {
            float timeout = Time.realtimeSinceStartup + 2f;
            while (!predicate() && Time.realtimeSinceStartup < timeout)
                yield return null;

            Assert.That(predicate(), Is.True, "Timed out waiting for PlayMode presentation behavior.");
        }

        private static BlobState Blob(string id, BlobColor color, int x, int y)
        {
            return new BlobState(id, BlobType.Normal, new GridPosition(x, y))
                .WithColor(color);
        }

        private static GameSessionSnapshot EmptySnapshot()
        {
            return new GameSessionSnapshot(
                "playmode-empty",
                new BoardState(
                    1,
                    1,
                    Array.Empty<BlobState>(),
                    Array.Empty<TileState>(),
                    new[] { new GridPosition(0, 0) }),
                0,
                true);
        }

        private static GameSessionSnapshot Snapshot(
            int width,
            int height,
            params BlobState[] blobs)
        {
            return new GameSessionSnapshot(
                "playmode-presentation",
                new BoardState(width, height, blobs, Array.Empty<TileState>()),
                0,
                false);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "Missing field: " + fieldName);
            field.SetValue(target, value);
        }

        private sealed class FakeGameplayState : IGameplayState
        {
            private readonly GameSessionSnapshot _snapshot;

            public FakeGameplayState(GameSessionSnapshot snapshot)
            {
                _snapshot = snapshot;
            }

            public event Action<GameSessionSnapshot> SnapshotChanged
            {
                add { }
                remove { }
            }

            public event Action<MoveResult> MoveResolved
            {
                add { }
                remove { }
            }

            public event Action<GameSessionSnapshot> StateRestored
            {
                add { }
                remove { }
            }

            public event Action<BlobSelectionResult> BlobSelected
            {
                add { }
                remove { }
            }

            public GameSessionSnapshot CreateSnapshot()
            {
                return _snapshot;
            }
        }

        private sealed class RuntimeBlobViewFactory : IBlobViewFactory
        {
            private readonly LevelColorPaletteAsset _palette;

            public RuntimeBlobViewFactory(LevelColorPaletteAsset palette)
            {
                _palette = palette;
            }

            public BlobView Create(
                BlobState blob,
                Transform parent,
                float cellSize,
                Vector2 origin)
            {
                var viewObject = new GameObject("Runtime Blob " + blob.Id);
                viewObject.transform.SetParent(parent, false);
                var visualObject = new GameObject("Visual");
                visualObject.transform.SetParent(viewObject.transform, false);
                SortingGroup sortingGroup = viewObject.AddComponent<SortingGroup>();
                BlobView view = viewObject.AddComponent<BlobView>();
                viewObject.AddComponent<BlobMotionAnimator>();
                SetPrivateField(view, "_visualRoot", visualObject.transform);
                SetPrivateField(view, "_sortingGroup", sortingGroup);
                view.Initialize(blob, _palette, cellSize, origin);
                return view;
            }
        }

        private sealed class RecordingStepHandler : IMoveStepPresentationHandler
        {
            private readonly List<string> _events;
            private int _stepNumber;

            public RecordingStepHandler(List<string> events)
            {
                _events = events;
            }

            public bool RepresentsMovement => false;

            public bool CanPresent(MoveStep step)
            {
                return step != null &&
                    step.Kind == MoveStepKind.Traverse &&
                    step.Effects.Count == 0;
            }

            public bool Present(
                MoveStep step,
                BoardEffectPresentationContext context,
                out IReadOnlyList<IBoardEffect> handledEffects)
            {
                int stepNumber = ++_stepNumber;
                context.Timeline.AppendCallback(
                    () => _events.Add("start-" + stepNumber));
                context.Timeline.InsertCallback(
                    0.04f,
                    () => _events.Add("end-" + stepNumber));
                handledEffects = Array.Empty<IBoardEffect>();
                return true;
            }
        }

        private sealed class RecordingContactFeedback : MonoBehaviour, IBlobContactFeedback
        {
            public int InvocationCount { get; private set; }
            public BlobContactFeedbackContext LastContext { get; private set; }

            public void PlayContactFeedback(BlobContactFeedbackContext context)
            {
                InvocationCount++;
                LastContext = context;
            }
        }
    }
}
