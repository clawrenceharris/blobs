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
    public abstract class PresentationTestFixture
    {
        protected readonly List<Object> _createdObjects = new();

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

        protected SpriteRenderer AddFadeGroup(BlobView view, out SpriteRenderer halo)
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

        protected BoardPresenter CreatePresenter(
            GameSessionSnapshot initialSnapshot,
            FakeGameplayState state = null)
        {
            GameObject root = CreateGameObject("PlayMode Board Presenter", active: false);
            root.AddComponent<MergeAnimationOrchestrator>();
            var surface = new GameObject("Board Surface");
            surface.transform.SetParent(root.transform, false);
            SetPrivateField(surface.AddComponent<BoardSurfaceView>(), "spriteSet",
                Resources.Load<BoardSurfaceSpriteSet>("BoardSurfaceSpriteSet"));
            BoardPresenter presenter = root.AddComponent<BoardPresenter>();
            var animationSettings = ConfigureAnimationSettings(root);
            LevelColorPaletteAsset palette = CreatePalette();
            var factory = new RuntimeBlobViewFactory(palette, animationSettings);

            root.SetActive(true);
            presenter.Initialize(
                state ?? new FakeGameplayState(initialSnapshot),
                palette,
                factory);
            return presenter;
        }

        protected BlobAnimationSettingsAsset ConfigureAnimationSettings(GameObject root)
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

        protected LevelDefinitionAsset CreateEmptyLevel(LevelColorPaletteAsset palette)
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

        protected LevelColorPaletteAsset CreatePalette()
        {
            return CreateAsset<LevelColorPaletteAsset>();
        }

        protected T CreateAsset<T>() where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            _createdObjects.Add(asset);
            return asset;
        }

        protected GameObject CreateGameObject(string name, bool active = true)
        {
            var gameObject = new GameObject(name);
            gameObject.SetActive(active);
            _createdObjects.Add(gameObject);
            return gameObject;
        }

        protected static IEnumerator WaitUntil(Func<bool> predicate)
        {
            float timeout = Time.realtimeSinceStartup + 2f;
            while (!predicate() && Time.realtimeSinceStartup < timeout)
                yield return null;

            Assert.That(predicate(), Is.True, "Timed out waiting for PlayMode presentation behavior.");
        }

        protected static BlobState Blob(string id, BlobColor color, int x, int y)
        {
            return new BlobState(id, BlobType.Normal, new GridPosition(x, y))
                .WithColor(color);
        }

        protected static GameSessionSnapshot EmptySnapshot()
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

        protected static GameSessionSnapshot Snapshot(
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

        protected static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "Missing field: " + fieldName);
            field.SetValue(target, value);
        }

        protected sealed class FakeGameplayState : IGameplayState
        {
            private readonly GameSessionSnapshot _snapshot;

            public FakeGameplayState(GameSessionSnapshot snapshot)
            {
                _snapshot = snapshot;
            }

            public event Action<GameSessionSnapshot> SnapshotChanged;
            public event Action<MoveResult> MoveResolved;
            public event Action<GameSessionSnapshot> StateRestored;
            public event Action<UndoResult> UndoResolved;
            public event Action<BlobSelectionResult> BlobSelected;

            public GameSessionSnapshot CreateSnapshot()
            {
                return _snapshot;
            }

            public bool CanUndo => false;

            public void RaiseUndoResolved(UndoResult undo)
            {
                UndoResolved?.Invoke(undo);
            }
        }

        protected sealed class RuntimeBlobViewFactory : IBlobViewFactory
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

        protected sealed class RecordingStepHandler : IMoveStepPresentationHandler
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

            public bool PresentReverse(
                MoveStep step,
                BoardEffectPresentationContext context,
                out IReadOnlyList<IBoardEffect> handledEffects)
            {
                handledEffects = Array.Empty<IBoardEffect>();
                return false;
            }
        }

        protected sealed class RecordingContactFeedback : MonoBehaviour, IBlobContactFeedback
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
