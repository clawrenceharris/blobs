using System;
using System.Collections.Generic;
using Blobs.Application;
using Blobs.Content;
using Blobs.Core;
using Blobs.Presentation;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Blobs.Tests.EditMode
{
    public sealed class MilestoneFourPresentationTests
    {
        private readonly List<Object> _createdObjects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int i = _createdObjects.Count - 1; i >= 0; i--)
            {
                if (_createdObjects[i] != null)
                    Object.DestroyImmediate(_createdObjects[i]);
            }

            _createdObjects.Clear();
        }

        [Test]
        public void OrderedRemoveMoveSpawnChainMatchesFinalSnapshot()
        {
            BlobState source = Blob("source", BlobColor.Red, 0, 0);
            BlobState target = Blob("target", BlobColor.Blue, 2, 0);
            BlobState spawned = Blob("spawned", BlobColor.Green, 0, 0);
            BoardPresenter presenter = CreatePresenter(Snapshot(source, target));

            var effects = new IBoardEffect[]
            {
                new RemoveBlobEffect(target),
                new MoveBlobEffect(source.Id, source.Position, target.Position),
                new SpawnBlobEffect(spawned)
            };
            GameSessionSnapshot finalSnapshot = Snapshot(
                source.WithPosition(target.Position),
                spawned);

            presenter.ApplyEffects(effects, finalSnapshot);

            Assert.That(presenter.VisibleBlobCount, Is.EqualTo(2));
            Assert.That(presenter.TryGetBlobView("target", out _), Is.False);
            Assert.That(presenter.TryGetBlobView("source", out BlobView sourceView), Is.True);
            Assert.That(sourceView.GridPosition, Is.EqualTo(new GridPosition(2, 0)));
            Assert.That(presenter.TryGetBlobView("spawned", out BlobView spawnedView), Is.True);
            Assert.That(spawnedView.GridPosition, Is.EqualTo(new GridPosition(0, 0)));
            Assert.That(presenter.IsSynchronizedWith(finalSnapshot), Is.True);
        }

        [Test]
        public void StepTimelineWithTraverseSpawnAndMergeBeatsMatchesFinalSnapshot()
        {
            BlobState source = Blob("source", BlobColor.Red, 0, 0);
            BlobState target = Blob("target", BlobColor.Blue, 2, 0);
            BlobState spawned = Blob("spawned", BlobColor.Green, 0, 0);
            BoardPresenter presenter = CreatePresenter(Snapshot(source, target));

            var steps = new[]
            {
                // Traverse beat: mover leaves 0,0 while the trail blob spawns there.
                new MoveStep(
                    MoveStepKind.Traverse,
                    new IBoardEffect[]
                    {
                        new MoveBlobEffect(source.Id, source.Position, new GridPosition(1, 0)),
                        new SpawnBlobEffect(spawned)
                    }),
                // Merge beat: occupant clears as the mover arrives on its tile.
                new MoveStep(
                    MoveStepKind.Merge,
                    new IBoardEffect[]
                    {
                        new RemoveBlobEffect(target),
                        new MoveBlobEffect(source.Id, new GridPosition(1, 0), target.Position)
                    })
            };
            GameSessionSnapshot finalSnapshot = Snapshot(
                source.WithPosition(target.Position),
                spawned);

            presenter.ApplySteps(steps, finalSnapshot);

            Assert.That(presenter.VisibleBlobCount, Is.EqualTo(2));
            Assert.That(presenter.TryGetBlobView("target", out _), Is.False);
            Assert.That(presenter.TryGetBlobView("source", out BlobView sourceView), Is.True);
            Assert.That(sourceView.GridPosition, Is.EqualTo(new GridPosition(2, 0)));
            Assert.That(presenter.TryGetBlobView("spawned", out BlobView spawnedView), Is.True);
            Assert.That(spawnedView.GridPosition, Is.EqualTo(new GridPosition(0, 0)));
            Assert.That(presenter.IsSynchronizedWith(finalSnapshot), Is.True);
        }

        [Test]
        public void StepWithMissingViewFallsBackToSnapshotRebuild()
        {
            GameSessionSnapshot snapshot = Snapshot(Blob("source", BlobColor.Red, 0, 0));
            BoardPresenter presenter = CreatePresenter(snapshot);
            presenter.TryGetBlobView("source", out BlobView originalView);

            presenter.ApplySteps(
                new[]
                {
                    new MoveStep(
                        MoveStepKind.Traverse,
                        new IBoardEffect[]
                        {
                            new MoveBlobEffect("missing", new GridPosition(0, 0), new GridPosition(1, 0))
                        })
                },
                snapshot);

            Assert.That(presenter.TryGetBlobView("source", out BlobView rebuiltView), Is.True);
            Assert.That(rebuiltView, Is.Not.SameAs(originalView));
            Assert.That(presenter.IsSynchronizedWith(snapshot), Is.True);
        }

        [Test]
        public void MissingMappedViewFallsBackToSnapshotRebuild()
        {
            GameSessionSnapshot snapshot = Snapshot(Blob("source", BlobColor.Red, 0, 0));
            BoardPresenter presenter = CreatePresenter(snapshot);
            presenter.TryGetBlobView("source", out BlobView originalView);

            presenter.ApplyEffects(
                new IBoardEffect[]
                {
                    new MoveBlobEffect("missing", new GridPosition(0, 0), new GridPosition(1, 0))
                },
                snapshot);

            Assert.That(presenter.TryGetBlobView("source", out BlobView rebuiltView), Is.True);
            Assert.That(rebuiltView, Is.Not.SameAs(originalView));
            Assert.That(presenter.IsSynchronizedWith(snapshot), Is.True);
        }

        [Test]
        public void UnsupportedEffectFallsBackToAuthoritativeSnapshot()
        {
            BoardPresenter presenter = CreatePresenter(Snapshot(Blob("old", BlobColor.Red, 0, 0)));
            GameSessionSnapshot authoritative = Snapshot(Blob("restored", BlobColor.Purple, 2, 1));

            presenter.ApplyEffects(
                new IBoardEffect[] { new UnsupportedEffect() },
                authoritative);

            Assert.That(presenter.TryGetBlobView("old", out _), Is.False);
            Assert.That(presenter.TryGetBlobView("restored", out BlobView restored), Is.True);
            Assert.That(restored.GridPosition, Is.EqualTo(new GridPosition(2, 1)));
            Assert.That(presenter.IsSynchronizedWith(authoritative), Is.True);
        }

        private BoardPresenter CreatePresenter(GameSessionSnapshot initialSnapshot)
        {
            var gameObject = new GameObject("Milestone 4 Board Presenter");
            _createdObjects.Add(gameObject);
            var palette = ScriptableObject.CreateInstance<LevelColorPaletteAsset>();
            _createdObjects.Add(palette);
            var blobColorPalette = ScriptableObject.CreateInstance<LevelColorPaletteAsset>();
            _createdObjects.Add(blobColorPalette);
            var presenter = gameObject.AddComponent<BoardPresenter>();
            presenter.Initialize(
                new FakeGameplayState(initialSnapshot),
                palette,
                new TestBlobViewFactory(blobColorPalette));
            return presenter;
        }

        private static BlobState Blob(string id, BlobColor color, int x, int y)
        {
            return new BlobState(id, BlobType.Normal, new GridPosition(x, y))
            .WithColor(color);
        }

        private static GameSessionSnapshot Snapshot(params BlobState[] blobs)
        {
            return new GameSessionSnapshot(
                "milestone-four-test",
                new BoardState(2, 1, blobs, new List<TileState>()),
                0,
                false);
        }

        private sealed class UnsupportedEffect : IBoardEffect
        {
            public void Apply(BoardState board)
            {
            }
        }

        private sealed class TestBlobViewFactory : IBlobViewFactory
        {
            private readonly LevelColorPaletteAsset _colorPalette;

            public TestBlobViewFactory(LevelColorPaletteAsset colorPalette)
            {
                _colorPalette = colorPalette;
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
                view.Initialize(blob, _colorPalette, cellSize, origin);
                return view;
            }
        }

        private sealed class FakeGameplayState : IGameplayState
        {
            private readonly GameSessionSnapshot _snapshot;
            public event Action<BlobSelectionResult> BlobSelected;

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

            public GameSessionSnapshot CreateSnapshot()
            {
                return _snapshot;
            }
        }
    }
}
