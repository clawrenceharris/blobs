using System;
using System.Collections.Generic;
using Blobs.Application;
using Blobs.Content;
using Blobs.Core;
using Blobs.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Blobs.Tests.EditMode
{
    public sealed class PresenterSeparationTests
    {
        private GameObject _root;
        private LevelColorPaletteAsset _palette;

        [TearDown]
        public void TearDown()
        {
            if (_root != null)
                UnityEngine.Object.DestroyImmediate(_root);
            if (_palette != null)
                UnityEngine.Object.DestroyImmediate(_palette);
        }

        [Test]
        public void BoardPresenterDelegatesViewTrackingAndRemovalToFocusedPresenters()
        {
            BlobState blob = new BlobState(
                "blob",
                BlobType.Normal,
                new GridPosition(0, 0)).WithColor(BlobColor.Red);
            TileState tile = new TileState(
                "tile",
                new GridPosition(1, 0),
                TileType.Sigil);
            var snapshot = new GameSessionSnapshot(
                "presenter-separation",
                new BoardState(2, 1, new[] { blob }, new[] { tile }),
                0,
                false);

            _root = new GameObject("Presenter Separation Test");
            _palette = ScriptableObject.CreateInstance<LevelColorPaletteAsset>();
            BoardPresenter boardPresenter = TestPresentationComposition.AddBoardPresenter(_root);
            boardPresenter.Initialize(
                new FakeGameplayState(snapshot),
                _palette,
                new TestBlobViewFactory(_palette),
                new TestTileViewFactory());

            BlobPresenter blobPresenter = _root.GetComponent<BlobPresenter>();
            TilePresenter tilePresenter = _root.GetComponent<TilePresenter>();
            BoardSurfacePresenter surfacePresenter =
                _root.GetComponent<BoardSurfacePresenter>();

            Assert.That(blobPresenter, Is.Not.Null);
            Assert.That(tilePresenter, Is.Not.Null);
            Assert.That(surfacePresenter, Is.Not.Null);
            Assert.That(blobPresenter.VisibleCount, Is.EqualTo(1));
            Assert.That(tilePresenter.VisibleCount, Is.EqualTo(1));

            Assert.That(blobPresenter.Remove(blob.Id), Is.True);
            Assert.That(tilePresenter.Remove(tile.Id), Is.True);
            Assert.That(boardPresenter.VisibleBlobCount, Is.Zero);
            Assert.That(boardPresenter.VisibleTileCount, Is.Zero);
        }

        [Test]
        public void BlobViewRoutesContactToEveryComposedFeedbackBehavior()
        {
            _root = new GameObject("Contact Feedback Test");
            var first = _root.AddComponent<RecordingContactFeedback>();
            var second = _root.AddComponent<RecordingContactFeedback>();
            BlobView target = _root.AddComponent<BlobView>();
            target.Initialize(
                new BlobState("target", BlobType.Rock, new GridPosition(0, 0)),
                null,
                1f,
                Vector2.zero);

            target.PlayContactFeedback(source: null);

            Assert.That(first.PlayCount, Is.EqualTo(1));
            Assert.That(second.PlayCount, Is.EqualTo(1));
            Assert.That(first.LastContext.Target, Is.SameAs(target));
        }

        [Test]
        public void RockPrefabComposesContactAudioFeedback()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Game/Prefabs/Blobs/PF_RockBlob.prefab");

            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.GetComponent<BlobContactAudioFeedback>(), Is.Not.Null);
        }

        [Test]
        public void AudioFeedbackDoesNotCreateMissingAudioSourcesAtPlaybackTime()
        {
            _root = new GameObject("Authored Audio Source Test");
            var contactFeedback = _root.AddComponent<BlobContactAudioFeedback>();
            var mergeFeedback = _root.AddComponent<MergeImpactAudioFeedback>();
            AudioClip clip = AudioClip.Create("Configuration Test", 16, 1, 8000, false);

            try
            {
                SetPrivateField(contactFeedback, "clip", clip);
                SetPrivateField(mergeFeedback, "_clip", clip);

                LogAssert.Expect(
                    LogType.Error,
                    "Blob contact audio on 'Authored Audio Source Test' has a clip " +
                    "but no authored AudioSource.");
                contactFeedback.PlayContactFeedback(default);

                LogAssert.Expect(
                    LogType.Error,
                    "Merge impact audio on 'Authored Audio Source Test' has a clip " +
                    "but no authored AudioSource.");
                mergeFeedback.PlayImpact(default);

                Assert.That(_root.GetComponent<AudioSource>(), Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(clip);
            }
        }

        [Test]
        public void TileViewRoutesStateToEveryComposedBinding()
        {
            _root = new GameObject("Tile State Binding Test");
            var first = _root.AddComponent<RecordingTileStateBinding>();
            var second = _root.AddComponent<RecordingTileStateBinding>();
            TileView view = _root.AddComponent<TileView>();
            var tile = new TileState(
                "tile",
                new GridPosition(1, 2),
                TileType.Sigil);

            view.Initialize(tile, 1f, Vector2.zero);

            Assert.That(first.BindCount, Is.EqualTo(1));
            Assert.That(second.BindCount, Is.EqualTo(1));
            Assert.That(first.LastContext.View, Is.SameAs(view));
            Assert.That(first.LastContext.State, Is.SameAs(tile));
            Assert.That(view.TileType, Is.EqualTo(TileType.Sigil));
        }


        [Test]
        public void BoardSurfacePresenterWithoutAuthoredViewFailsClearly()
        {
            _root = new GameObject("Missing Board Surface View Test");
            BoardSurfacePresenter presenter = _root.AddComponent<BoardSurfacePresenter>();

            InvalidOperationException error = Assert.Throws<InvalidOperationException>(
                () => presenter.Initialize(1f, Vector2.zero));

            StringAssert.Contains("requires an authored BoardSurfaceView", error.Message);
            Assert.That(_root.transform.childCount, Is.Zero);
        }

        [Test]
        public void BoardSynchronizationRejectsChangedBlobPresentationTraits()
        {
            BlobState initial = new BlobState(
                "blob",
                BlobType.Trail,
                new GridPosition(0, 0))
                .WithColor(BlobColor.Red)
                .WithTrail(BlobColor.Blue);
            var initialSnapshot = new GameSessionSnapshot(
                "presentation-state-sync",
                new BoardState(1, 1, new[] { initial }, Array.Empty<TileState>()),
                0,
                false);

            _root = new GameObject("Presentation State Synchronization Test");
            _palette = ScriptableObject.CreateInstance<LevelColorPaletteAsset>();
            BoardPresenter presenter = TestPresentationComposition.AddBoardPresenter(_root);
            presenter.Initialize(
                new FakeGameplayState(initialSnapshot),
                _palette,
                new TestBlobViewFactory(_palette),
                new TestTileViewFactory());

            Assert.That(presenter.IsSynchronizedWith(initialSnapshot), Is.True);
            Assert.That(
                presenter.IsSynchronizedWith(SnapshotWith(
                    new BlobState(
                        initial.Id,
                        BlobType.Rock,
                        initial.Position,
                        initial.Components))),
                Is.False,
                "Prefab-changing blob types must invalidate synchronization.");
            Assert.That(
                presenter.IsSynchronizedWith(SnapshotWith(
                    initial.WithColor(BlobColor.Green))),
                Is.False,
                "Body color changes must invalidate synchronization.");
            Assert.That(
                presenter.IsSynchronizedWith(SnapshotWith(
                    initial.WithTrail(BlobColor.Purple))),
                Is.False,
                "Prefab-specific trail color changes must invalidate synchronization.");
        }

        [Test]
        public void SigilTilePrefabIsRegisteredInProductionCatalog()
        {
            ViewCatalogAsset catalog = AssetDatabase.LoadAssetAtPath<ViewCatalogAsset>(
                "Assets/_Game/Content/Presentation/TileViewCatalog.asset");

            Assert.That(catalog, Is.Not.Null);
            TileView prefab = catalog.GetRequiredTilePrefab(TileType.Sigil);
            Assert.That(prefab, Is.Not.Null);
        }

        [Test]
        public void BlobPresenterDeclaresMergeOrchestratorComposition()
        {
            _root = new GameObject("Blob Presenter Composition Test");

            _root.AddComponent<BlobPresenter>();

            Assert.That(_root.GetComponent<MergeAnimationOrchestrator>(), Is.Not.Null);
        }

        [Test]
        public void MergeOrchestratorRoutesImpactToEveryComposedFeedbackChannel()
        {
            _root = new GameObject("Merge Feedback Channels Test");
            var first = _root.AddComponent<RecordingMergeImpactFeedback>();
            var second = _root.AddComponent<RecordingMergeImpactFeedback>();
            MergeAnimationOrchestrator orchestrator =
                _root.AddComponent<MergeAnimationOrchestrator>();
            Vector3 impactPosition = new(2f, 3f, 0f);

            orchestrator.PlayImpact(impactPosition, Color.magenta, sortingAnchor: null);

            Assert.That(first.PlayCount, Is.EqualTo(1));
            Assert.That(second.PlayCount, Is.EqualTo(1));
            Assert.That(first.LastContext.WorldPosition, Is.EqualTo(impactPosition));
            Assert.That(first.LastContext.BlobColor, Is.EqualTo(Color.magenta));
        }

        [Test]
        public void RegisteredEffectHandlerExtendsBoardPresentationWithoutCoordinatorChanges()
        {
            BlobState blob = new BlobState(
                "blob",
                BlobType.Normal,
                new GridPosition(0, 0)).WithColor(BlobColor.Red);
            var snapshot = new GameSessionSnapshot(
                "custom-effect-presentation",
                new BoardState(1, 1, new[] { blob }, Array.Empty<TileState>()),
                0,
                false);

            _root = new GameObject("Effect Handler Test");
            _palette = ScriptableObject.CreateInstance<LevelColorPaletteAsset>();
            BoardPresenter presenter = TestPresentationComposition.AddBoardPresenter(_root);
            var handler = new RecordingEffectHandler();
            presenter.RegisterEffectHandler(handler);
            presenter.Initialize(
                new FakeGameplayState(snapshot),
                _palette,
                new TestBlobViewFactory(_palette),
                new TestTileViewFactory());
            presenter.TryGetBlobView(blob.Id, out BlobView originalView);

            var effect = new RecordingEffect();
            presenter.ApplyEffects(new IBoardEffect[] { effect }, snapshot);
            presenter.ApplySteps(
                new[]
                {
                    new MoveStep(
                        MoveStepKind.Traverse,
                        new IBoardEffect[] { effect })
                },
                snapshot);

            Assert.That(handler.OrderedCount, Is.EqualTo(1));
            Assert.That(handler.BeatCount, Is.EqualTo(1));
            Assert.That(presenter.TryGetBlobView(blob.Id, out BlobView currentView), Is.True);
            Assert.That(currentView, Is.SameAs(originalView));
        }

        [Test]
        public void RegisteredMoveStepHandlerOwnsCompositeEffectsWithoutCoordinatorChanges()
        {
            BlobState blob = new BlobState(
                "blob",
                BlobType.Normal,
                new GridPosition(0, 0)).WithColor(BlobColor.Red);
            var snapshot = new GameSessionSnapshot(
                "custom-step-presentation",
                new BoardState(1, 1, new[] { blob }, Array.Empty<TileState>()),
                0,
                false);

            _root = new GameObject("Move Step Handler Test");
            _palette = ScriptableObject.CreateInstance<LevelColorPaletteAsset>();
            BoardPresenter presenter = TestPresentationComposition.AddBoardPresenter(_root);
            var effectHandler = new RecordingEffectHandler();
            var stepHandler = new RecordingMoveStepHandler();
            presenter.RegisterEffectHandler(effectHandler);
            presenter.RegisterMoveStepHandler(stepHandler);
            presenter.Initialize(
                new FakeGameplayState(snapshot),
                _palette,
                new TestBlobViewFactory(_palette),
                new TestTileViewFactory());
            presenter.TryGetBlobView(blob.Id, out BlobView originalView);

            var handledEffect = new RecordingEffect();
            var remainingEffect = new RecordingEffect();
            presenter.ApplySteps(
                new[]
                {
                    new MoveStep(
                        MoveStepKind.Traverse,
                        new IBoardEffect[] { handledEffect, remainingEffect })
                },
                snapshot);

            Assert.That(stepHandler.PresentCount, Is.EqualTo(1));
            Assert.That(effectHandler.BeatCount, Is.EqualTo(1),
                "Only effects not consumed by the composite handler should use the generic pipeline.");
            Assert.That(presenter.TryGetBlobView(blob.Id, out BlobView currentView), Is.True);
            Assert.That(currentView, Is.SameAs(originalView));
        }

        [Test]
        public void SelectionPresenterUpdatesOnlyPreviouslyAndCurrentlySelectedViews()
        {
            BlobState first = new BlobState(
                "first",
                BlobType.Normal,
                new GridPosition(0, 0)).WithColor(BlobColor.Red);
            BlobState second = new BlobState(
                "second",
                BlobType.Normal,
                new GridPosition(1, 0)).WithColor(BlobColor.Blue);
            BlobState unrelated = new BlobState(
                "unrelated",
                BlobType.Normal,
                new GridPosition(2, 0)).WithColor(BlobColor.Green);
            var snapshot = new GameSessionSnapshot(
                "central-selection-presentation",
                new BoardState(
                    3,
                    1,
                    new[] { first, second, unrelated },
                    Array.Empty<TileState>()),
                0,
                false);
            var state = new FakeGameplayState(snapshot);

            _root = new GameObject("Central Selection Presenter Test");
            _palette = ScriptableObject.CreateInstance<LevelColorPaletteAsset>();
            BoardPresenter presenter = TestPresentationComposition.AddBoardPresenter(_root);
            presenter.Initialize(
                state,
                _palette,
                new TestBlobViewFactory(_palette, includeMotionAnimator: true),
                new TestTileViewFactory());
            presenter.TryGetBlobView(first.Id, out BlobView firstView);
            presenter.TryGetBlobView(second.Id, out BlobView secondView);
            presenter.TryGetBlobView(unrelated.Id, out BlobView unrelatedView);

            state.PublishSelection(BlobSelectionResult.Selected(first.Id, null));
            unrelatedView.BlobMotionAnimator.SetMoving();
            state.PublishSelection(BlobSelectionResult.Selected(second.Id, null));

            Assert.That(state.BlobSelectionSubscriberCount, Is.EqualTo(1));
            Assert.That(firstView.BlobMotionAnimator.CurrentState, Is.EqualTo(BlobAnimationState.Idle));
            Assert.That(secondView.BlobMotionAnimator.CurrentState, Is.EqualTo(BlobAnimationState.Selected));
            Assert.That(unrelatedView.BlobMotionAnimator.CurrentState, Is.EqualTo(BlobAnimationState.Moving));

            UnityEngine.Object.DestroyImmediate(_root);
            _root = null;
            Assert.That(state.BlobSelectionSubscriberCount, Is.Zero);
        }

        private sealed class TestBlobViewFactory : IBlobViewFactory
        {
            private readonly LevelColorPaletteAsset _palette;
            private readonly bool _includeMotionAnimator;

            public TestBlobViewFactory(
                LevelColorPaletteAsset palette,
                bool includeMotionAnimator = false)
            {
                _palette = palette;
                _includeMotionAnimator = includeMotionAnimator;
            }

            public BlobView Create(
                BlobState blob,
                Transform parent,
                float cellSize,
                Vector2 origin)
            {
                var gameObject = new GameObject("Test Blob " + blob.Id);
                gameObject.transform.SetParent(parent, false);
                BlobView view = gameObject.AddComponent<BlobView>();
                if (_includeMotionAnimator)
                    gameObject.AddComponent<BlobMotionAnimator>();
                view.Initialize(blob, _palette, cellSize, origin);
                return view;
            }
        }

        private static GameSessionSnapshot SnapshotWith(BlobState blob)
        {
            return new GameSessionSnapshot(
                "presentation-state-sync",
                new BoardState(1, 1, new[] { blob }, Array.Empty<TileState>()),
                0,
                false);
        }

        private sealed class TestTileViewFactory : ITileViewFactory
        {
            public TileView Create(
                TileState tile,
                Transform parent,
                float cellSize,
                Vector2 origin)
            {
                var gameObject = new GameObject("Test Tile " + tile.Id);
                gameObject.transform.SetParent(parent, false);
                TileView view = gameObject.AddComponent<TileView>();
                view.Initialize(tile, cellSize, origin);
                return view;
            }
        }

        private sealed class FakeGameplayState : IGameplayState
        {
            private readonly GameSessionSnapshot _snapshot;
            private event Action<BlobSelectionResult> _blobSelected;

            public FakeGameplayState(GameSessionSnapshot snapshot)
            {
                _snapshot = snapshot;
            }

            public int BlobSelectionSubscriberCount =>
                _blobSelected?.GetInvocationList().Length ?? 0;

#pragma warning disable CS0067
            public event Action<GameSessionSnapshot> SnapshotChanged;
            public event Action<MoveResult> MoveResolved;
            public event Action<GameSessionSnapshot> StateRestored;
#pragma warning restore CS0067

            public event Action<BlobSelectionResult> BlobSelected
            {
                add => _blobSelected += value;
                remove => _blobSelected -= value;
            }

            public void PublishSelection(BlobSelectionResult result)
            {
                _blobSelected?.Invoke(result);
            }

            public GameSessionSnapshot CreateSnapshot()
            {
                return _snapshot;
            }
        }

        private sealed class RecordingContactFeedback : MonoBehaviour, IBlobContactFeedback
        {
            public int PlayCount { get; private set; }
            public BlobContactFeedbackContext LastContext { get; private set; }

            public void PlayContactFeedback(BlobContactFeedbackContext context)
            {
                PlayCount++;
                LastContext = context;
            }
        }

        private sealed class RecordingTileStateBinding : MonoBehaviour, ITileStateBinding
        {
            public int BindCount { get; private set; }
            public TilePresentationContext LastContext { get; private set; }

            public void Bind(TilePresentationContext context)
            {
                BindCount++;
                LastContext = context;
            }
        }

        private sealed class RecordingMergeImpactFeedback :
            MonoBehaviour,
            IMergeImpactFeedback
        {
            public int PlayCount { get; private set; }
            public MergeImpactFeedbackContext LastContext { get; private set; }

            public void PlayImpact(MergeImpactFeedbackContext context)
            {
                PlayCount++;
                LastContext = context;
            }
        }

        private sealed class RecordingEffect : IBoardEffect
        {
            public void Apply(BoardState board)
            {
            }
        }

        private sealed class RecordingEffectHandler :
            BoardEffectPresentationHandler<RecordingEffect>
        {
            public int OrderedCount { get; private set; }
            public int BeatCount { get; private set; }

            public override BoardEffectPresentationPhase Phase =>
                BoardEffectPresentationPhase.Aftermath;

            protected override bool PresentOrdered(
                RecordingEffect effect,
                BoardEffectPresentationContext context)
            {
                OrderedCount++;
                return true;
            }

            protected override bool PresentInBeat(
                RecordingEffect effect,
                BoardEffectPresentationContext context)
            {
                BeatCount++;
                return true;
            }
        }

        private sealed class RecordingMoveStepHandler : IMoveStepPresentationHandler
        {
            public int PresentCount { get; private set; }
            public bool RepresentsMovement => true;

            public bool CanPresent(MoveStep step)
            {
                return step.Kind == MoveStepKind.Traverse &&
                    step.Effects.Count > 0 &&
                    step.Effects[0] is RecordingEffect;
            }

            public bool Present(
                MoveStep step,
                BoardEffectPresentationContext context,
                out IReadOnlyList<IBoardEffect> handledEffects)
            {
                PresentCount++;
                handledEffects = new[] { step.Effects[0] };
                return true;
            }
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            System.Reflection.FieldInfo field = target.GetType().GetField(
                fieldName,
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }
    }
}
