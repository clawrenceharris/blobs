using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
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
    public sealed partial class PresentationRuntimeTests
    {
        private readonly List<Object> _createdObjects = new();

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            // Scene objects must finish cleanup while their settings assets still exist.
            for (int i = _createdObjects.Count - 1; i >= 0; i--)
                if (_createdObjects[i] is GameObject gameObject && gameObject != null)
                    Object.Destroy(gameObject);
            yield return null;

            for (int i = _createdObjects.Count - 1; i >= 0; i--)
                if (_createdObjects[i] != null)
                    Object.Destroy(_createdObjects[i]);

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
                            MergeEffect.NormalMerge(new MoveContext(
                                initial.Board, MovePlan.Move(source, target), source, target))
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
        public IEnumerator GhostReturnFadesBodyOnceAndKeepsHaloVisible()
        {
            BlobState source = Blob("source", BlobColor.Red, 0, 0);
            var ghost = new BlobState("ghost", BlobType.Ghost, new GridPosition(3, 0));
            var initial = Snapshot(4, 1, source, ghost);
            BoardPresenter presenter = CreatePresenter(initial);
            presenter.TryGetBlobView("ghost", out BlobView view);
            SpriteRenderer body = AddFadeGroup(view, out SpriteRenderer halo);
            Vector3 start = view.transform.localPosition;
            BoardState board = initial.Board.Clone();
            MoveResult result = new MoveResolver().Resolve(board, new MoveIntent(source, ghost));
            var final = new GameSessionSnapshot("ghost", board, 1, false);

            presenter.ApplySteps(result.Steps, final);
            yield return WaitUntil(() => body.color.a < 0.01f);
            Assert.That(halo.color.a, Is.EqualTo(1f));
            yield return WaitUntil(() => view.transform.localPosition.x < start.x - 0.1f);
            Assert.That(body.color.a, Is.LessThan(0.01f));
            yield return WaitUntil(() => !presenter.IsPresenting);
            Assert.That(body.color.a, Is.EqualTo(0.6f).Within(0.001f));
            Assert.That(halo.color.a, Is.EqualTo(1f));
            Assert.That(presenter.TryGetBlobView("ghost", out BlobView survivor), Is.True);
            Assert.That(survivor, Is.SameAs(view));
            Assert.That(presenter.IsSynchronizedWith(final), Is.True);
        }

        [UnityTest]
        public IEnumerator UndoDuringInvisibleReturnCancelsOldCompletion()
        {
            BlobState source = Blob("source", BlobColor.Red, 0, 0);
            var ghost = new BlobState("ghost", BlobType.Ghost, new GridPosition(3, 0));
            var initial = Snapshot(4, 1, source, ghost);
            BoardPresenter presenter = CreatePresenter(initial);
            presenter.TryGetBlobView("ghost", out BlobView view);
            SpriteRenderer body = AddFadeGroup(view, out _);
            BoardState board = initial.Board.Clone();
            var result = new MoveResolver().Resolve(board, new MoveIntent(source, ghost));
            int completed = 0;
            presenter.SnapshotChanged += _ => completed++;
            presenter.ApplySteps(result.Steps, new GameSessionSnapshot("ghost", board, 1, false));
            yield return WaitUntil(() => body.color.a < 0.01f);
            presenter.Rebuild(initial);
            yield return new WaitForSeconds(0.8f);
            Assert.That(presenter.IsPresenting, Is.False);
            Assert.That(presenter.IsSynchronizedWith(initial), Is.True);
            Assert.That(completed, Is.EqualTo(1), "Canceled playback must not publish its old snapshot.");
        }

        [UnityTest]
        public IEnumerator SigilReturnClearsOnlyAfterTravel()
        {
            BlobState source = Blob("source", BlobColor.Red, 0, 0);
            var ghost = new BlobState("ghost", BlobType.Ghost, new GridPosition(3, 0));
            var board = new BoardState(4, 1, new[] { source, ghost }, new[]
            {
                new TileState("sigil", new GridPosition(1, 0), TileType.Sigil)
            });
            // Use the normal test surface; tiles are tested independently by Core.
            var initial = Snapshot(4, 1, source, ghost);
            BoardPresenter presenter = CreatePresenter(initial);
            presenter.TryGetBlobView("ghost", out BlobView view);
            SpriteRenderer body = AddFadeGroup(view, out SpriteRenderer halo);
            var result = new MoveResolver().Resolve(board, new MoveIntent(source, ghost));
            var final = Snapshot(4, 1);
            presenter.ApplySteps(result.Steps, final);
            yield return WaitUntil(() => body != null && body.color.a < 0.01f);
            Assert.That(view != null, Is.True);
            Assert.That(halo.color.a, Is.EqualTo(1f));
            yield return WaitUntil(() => !presenter.IsPresenting);
            yield return null;
            Assert.That(view == null, Is.True);
            Assert.That(presenter.VisibleBlobCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator AwaitedPlaybackCancellationRestoresCommittedSnapshot()
        {
            var source = Blob("source", BlobColor.Red, 0, 0);
            var ghost = new BlobState("ghost", BlobType.Ghost, new GridPosition(3, 0));
            var initial = Snapshot(4, 1, source, ghost);
            BoardPresenter presenter = CreatePresenter(initial);
            BoardState board = initial.Board.Clone();
            var result = new MoveResolver().Resolve(board, new MoveIntent(source, ghost));
            var final = new GameSessionSnapshot("ghost", board, 1, false);
            using var cancellation = new CancellationTokenSource();
            var task = presenter.ApplyStepsAsync(result.Steps, final,
                cancellationToken: cancellation.Token).SuppressCancellationThrow();
            cancellation.Cancel();
            yield return task.ToCoroutine(wasCanceled => Assert.That(wasCanceled, Is.True));
            Assert.That(presenter.IsPresenting, Is.False);
            Assert.That(presenter.IsSynchronizedWith(final), Is.True);
        }

        [UnityTest]
        public IEnumerator LandingAbsorptionKeepsIntermediateTrailViews()
        {
            var source = new BlobState("source", BlobType.Trail, new GridPosition(0, 0))
                .WithColor(BlobColor.Red).WithTrail(BlobColor.Blue);
            var ghost = new BlobState("ghost", BlobType.Ghost, new GridPosition(3, 0));
            var initial = Snapshot(4, 1, source, ghost);
            var presenter = CreatePresenter(initial);
            var board = initial.Board.Clone();
            var result = new MoveResolver().Resolve(board, new MoveIntent(source, ghost));
            var final = new GameSessionSnapshot("ghost", board, 1, false);
            yield return presenter.ApplyStepsAsync(result.Steps, final).ToCoroutine();
            Assert.That(presenter.IsSynchronizedWith(final), Is.True);
            Assert.That(presenter.VisibleBlobCount, Is.EqualTo(3));
        }

        [UnityTest]
        public IEnumerator DisabledPresenterAppliesGhostEffectsImmediately()
        {
            var source = Blob("source", BlobColor.Red, 0, 0);
            var ghost = new BlobState("ghost", BlobType.Ghost, new GridPosition(3, 0));
            var initial = Snapshot(4, 1, source, ghost);
            var presenter = CreatePresenter(initial);
            presenter.enabled = false;
            var board = initial.Board.Clone();
            var result = new MoveResolver().Resolve(board, new MoveIntent(source, ghost));
            var final = new GameSessionSnapshot("ghost", board, 1, false);
            var playback = presenter.ApplyStepsAsync(result.Steps, final);
            Assert.That(playback.Status, Is.EqualTo(UniTaskStatus.Succeeded));
            Assert.That(presenter.IsSynchronizedWith(final), Is.True);
            yield return playback.ToCoroutine();
        }

        private SpriteRenderer AddFadeGroup(BlobView view, out SpriteRenderer halo)
        {
            var bodyObject = new GameObject("Body");
            bodyObject.transform.SetParent(view.transform, false);
            var body = bodyObject.AddComponent<SpriteRenderer>();
            body.color = new Color(1f, 1f, 1f, 0.6f);
            var haloObject = new GameObject("Halo");
            haloObject.transform.SetParent(view.transform, false);
            halo = haloObject.AddComponent<SpriteRenderer>();
            var fade = view.gameObject.AddComponent<FadeableVisual>();
            SetPrivateField(fade, "renderers", new[] { body });
            SetPrivateField(view.BlobRenderer, "fadeableVisual", fade);
            return body;
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
            ConfigureAnimationSettings(root);
            GameplayInputAdapter input = root.AddComponent<GameplayInputAdapter>();
            GameplayCommandAdapter commands = root.AddComponent<GameplayCommandAdapter>();
            GameBootstrapper bootstrapper = root.AddComponent<GameBootstrapper>();
            LevelColorPaletteAsset palette = CreatePalette();
            LevelDefinitionAsset level = CreateEmptyLevel(palette);
            ViewCatalogAsset viewCatalog = CreateAsset<ViewCatalogAsset>();

            SetPrivateField(board.GetComponent<BlobPresenter>(), "_viewCatalog", viewCatalog);
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
            var animationSettings = ConfigureAnimationSettings(root);
            LevelColorPaletteAsset palette = CreatePalette();
            var factory = new RuntimeBlobViewFactory(palette, animationSettings);

            root.SetActive(true);
            presenter.Initialize(
                new FakeGameplayState(initialSnapshot),
                palette,
                factory);
            return presenter;
        }

        private BlobAnimationSettingsAsset ConfigureAnimationSettings(GameObject root)
        {
            var settings = CreateAsset<BlobAnimationSettingsAsset>();
            settings.BlobSelectionSettings = new BlobSelectionSettings();
            settings.BlobIdleSettings = new BlobIdleSettings();
            settings.BlobMotionSettings = new BlobMotionSettings();
            settings.MergeImpactSettings = new BlobMergeImpactSettings();
            settings.GhostReturnSettings = new GhostReturnSettings();
            SetPrivateField(root.GetComponent<BlobPresenter>(), "blobAnimationSettings", settings);
            return settings;
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

            private readonly BlobAnimationSettingsAsset _settings;
            public RuntimeBlobViewFactory(LevelColorPaletteAsset palette, BlobAnimationSettingsAsset settings)
            {
                _palette = palette;
                _settings = settings;
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
                var animator = viewObject.AddComponent<BlobMotionAnimator>();
                SetPrivateField(animator, "_blobAnimationSettings", _settings);
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
