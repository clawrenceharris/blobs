using System;
using System.Collections.Generic;
using Blobs.Application;
using Blobs.Content;
using Blobs.Core;
using Blobs.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

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
                TileType.Normal);
            var snapshot = new GameSessionSnapshot(
                "presenter-separation",
                new BoardState(2, 1, new[] { blob }, new[] { tile }),
                0,
                false);

            _root = new GameObject("Presenter Separation Test");
            _palette = ScriptableObject.CreateInstance<LevelColorPaletteAsset>();
            BoardPresenter boardPresenter = _root.AddComponent<BoardPresenter>();
            boardPresenter.Initialize(
                new FakeGameplayState(snapshot),
                _palette,
                new TestBlobViewFactory(_palette));

            BlobPresenter blobPresenter = _root.GetComponent<BlobPresenter>();
            TilePresenter tilePresenter = _root.GetComponent<TilePresenter>();

            Assert.That(blobPresenter, Is.Not.Null);
            Assert.That(tilePresenter, Is.Not.Null);
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
            BoardPresenter presenter = _root.AddComponent<BoardPresenter>();
            var handler = new RecordingEffectHandler();
            presenter.RegisterEffectHandler(handler);
            presenter.Initialize(
                new FakeGameplayState(snapshot),
                _palette,
                new TestBlobViewFactory(_palette));
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

        private sealed class TestBlobViewFactory : IBlobViewFactory
        {
            private readonly LevelColorPaletteAsset _palette;

            public TestBlobViewFactory(LevelColorPaletteAsset palette)
            {
                _palette = palette;
            }

            public BlobView Create(
                BlobState blob,
                IGameplayState state,
                Transform parent,
                float cellSize,
                Vector2 origin)
            {
                var gameObject = new GameObject("Test Blob " + blob.Id);
                gameObject.transform.SetParent(parent, false);
                BlobView view = gameObject.AddComponent<BlobView>();
                view.Initialize(blob, state, _palette, cellSize, origin);
                return view;
            }
        }

        private sealed class FakeGameplayState : IGameplayState
        {
            private readonly GameSessionSnapshot _snapshot;

            public FakeGameplayState(GameSessionSnapshot snapshot)
            {
                _snapshot = snapshot;
            }

#pragma warning disable CS0067
            public event Action<GameSessionSnapshot> SnapshotChanged;
            public event Action<MoveResult> MoveResolved;
            public event Action<GameSessionSnapshot> StateRestored;
            public event Action<BlobSelectionResult> BlobSelected;
#pragma warning restore CS0067

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
    }
}
