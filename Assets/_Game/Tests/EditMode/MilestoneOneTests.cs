using System.Linq;
using Blobs.Application;
using Blobs.Core;
using NUnit.Framework;

namespace Blobs.Tests.EditMode
{
    public sealed class MilestoneOneTests
    {
        [Test]
        public void SameRowMatchingNormalMergeCompletesSamplePuzzle()
        {
            var session = new GameSession(new LevelDefinition(
                "same_row_matching_normal_merge",
                1,
                3,
                1,
                new[]
                {
                    NormalBlob("red_a", BlobColor.Red, 0, 0),
                    NormalBlob("blue_a", BlobColor.Blue, 2, 0)
                },
                new[]
                {
                    NormalTile("tile_a", 0, 0),
                    NormalTile("tile_b", 2, 0)
                }));

            var result = session.ExecuteMove(new MoveIntent("red_a", "blue_a"));
            var snapshot = session.CreateSnapshot();

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.FailureReason, Is.EqualTo(MoveFailureReason.None));
            Assert.That(result.Effects.Count, Is.EqualTo(3));
            Assert.That(result.InverseEffects.Count, Is.EqualTo(3));
            Assert.That(snapshot.Blobs.Count, Is.EqualTo(0));
            Assert.That(snapshot.Tiles.Count, Is.EqualTo(2));
            Assert.That(snapshot.Tiles.Select(tile => tile.Id), Is.EquivalentTo(new[] { "tile_a", "tile_b" }));
            Assert.That(snapshot.IsComplete, Is.True);
            Assert.That(snapshot.MoveCount, Is.EqualTo(1));
        }

        [Test]
        public void SameColumnMatchingNormalMergeUsesSameResolverPath()
        {
            var session = new GameSession(new LevelDefinition(
                "same_column",
                1,
                1,
                3,
                new[]
                {
                    NormalBlob("red_a", BlobColor.Red, 0, 0),
                    NormalBlob("blue_a", BlobColor.Blue, 0, 2)
                },
                new[]
                {
                    NormalTile("tile_a", 0, 0),
                    NormalTile("tile_b", 0, 2)
                }));

            var result = session.ExecuteMove(new MoveIntent("red_a", "blue_a"));

            Assert.That(result.Succeeded, Is.True);
            Assert.That(session.CreateSnapshot().IsComplete, Is.True);
        }

        [Test]
        public void NonAlignedMoveIsRejectedWithoutMutatingBoard()
        {
            var session = new GameSession(new LevelDefinition(
                "non_aligned",
                1,
                2,
                2,
                new[]
                {
                    NormalBlob("red_a", BlobColor.Red, 0, 0),
                    NormalBlob("red_b", BlobColor.Red, 1, 1)
                },
                new[]
                {
                    NormalTile("red_a", 0, 0),
                    NormalTile("red_b", 1, 1)
                }));

            var result = session.ExecuteMove(new MoveIntent("red_a", "red_b"));
            var snapshot = session.CreateSnapshot();

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.FailureReason, Is.EqualTo(MoveFailureReason.NotAligned));
            Assert.That(snapshot.Blobs.Count, Is.EqualTo(2));
            Assert.That(snapshot.MoveCount, Is.EqualTo(0));
            Assert.That(snapshot.IsComplete, Is.False);
        }

        [Test]
        public void SameColorMoveIsRejectedWithoutMutatingBoard()
        {
            var session = new GameSession(new LevelDefinition(
                "same_color_mismatch",
                1,
                2,
                1,
                new[]
                {
                    NormalBlob("red_a", BlobColor.Red, 0, 0),
                    NormalBlob("red_b", BlobColor.Red, 1, 0)
                },
                new[]
                {
                    NormalTile("tile_a", 0, 0),
                    NormalTile("tile_b", 1, 0)
                }));

            var result = session.ExecuteMove(new MoveIntent("red_a", "red_b"));
            var snapshot = session.CreateSnapshot();

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.FailureReason, Is.EqualTo(MoveFailureReason.ColorMismatch));
            Assert.That(snapshot.Blobs.Count, Is.EqualTo(2));
            Assert.That(snapshot.MoveCount, Is.EqualTo(0));
            Assert.That(snapshot.IsComplete, Is.False);
        }

        [Test]
        public void UndoAfterCompletionRestoresPreviousBoardAndIncompleteState()
        {
            var session = new GameSession(new LevelDefinition(
                "undo_after_completion",
                1,
                2,
                1,
                new[]
                {
                    NormalBlob("red_a", BlobColor.Red, 0, 0),
                    NormalBlob("blue_a", BlobColor.Blue, 1, 0)
                },
                new[]
                {
                    NormalTile("tile_a", 0, 0),
                    NormalTile("tile_b", 1, 0)
                }));
            session.ExecuteMove(new MoveIntent("red_a", "blue_a"));

            var undone = session.Undo();
            var snapshot = session.CreateSnapshot();

            Assert.That(undone, Is.True);
            Assert.That(snapshot.Blobs.Count, Is.EqualTo(2));
            Assert.That(snapshot.Blobs.Select(blob => blob.Id), Is.EquivalentTo(new[] { "red_a", "blue_a" }));
            Assert.That(snapshot.IsComplete, Is.False);
            Assert.That(snapshot.MoveCount, Is.EqualTo(0));
            Assert.That(snapshot.Tiles.Count, Is.EqualTo(2));
            Assert.That(snapshot.Tiles.Select(tile => tile.Id), Is.EquivalentTo(new[] { "tile_a", "tile_b" }));
            Assert.That(snapshot.CanUndo, Is.False);
        }

        [Test]
        public void UndoLastMoveReturnsInverseEffectsForPresentation()
        {
            var session = new GameSession(new LevelDefinition(
                "undo_result",
                1,
                2,
                1,
                new[]
                {
                    NormalBlob("red_a", BlobColor.Red, 0, 0),
                    NormalBlob("blue_a", BlobColor.Blue, 1, 0)
                },
                new[]
                {
                    NormalTile("tile_a", 0, 0),
                    NormalTile("tile_b", 1, 0)
                }));
            session.ExecuteMove(new MoveIntent("red_a", "blue_a"));

            var result = session.UndoLastMove();

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Effects.Count, Is.EqualTo(3));
            Assert.That(result.IsComplete, Is.False);
            Assert.That(session.CreateSnapshot().Blobs.Count, Is.EqualTo(2));
        }

        [Test]
        public void SelectingTwoDifferentBlobsExecutesMoveInSession()
        {
            var session = new GameSession(new LevelDefinition(
                "selection_executes_move",
                1,
                2,
                1,
                new[]
                {
                    NormalBlob("red_a", BlobColor.Red, 0, 0),
                    NormalBlob("blue_a", BlobColor.Blue, 1, 0)
                },
                new[]
                {
                    NormalTile("tile_a", 0, 0),
                    NormalTile("tile_b", 1, 0)
                }));

            var first = session.SelectBlobAt(new GridPosition(0, 0));
            var second = session.SelectBlobAt(new GridPosition(1, 0));

            Assert.That(first.HasSelection, Is.True);
            Assert.That(first.MoveAttempted, Is.False);
            Assert.That(second.MoveAttempted, Is.True);
            Assert.That(second.MoveResult.Succeeded, Is.True);
            Assert.That(session.CreateSnapshot().IsComplete, Is.True);
        }

        [Test]
        public void SelectingSameBlobTwiceClearsSelectionWithoutMove()
        {
            var session = new GameSession(new LevelDefinition(
                "selection_toggle",
                1,
                2,
                1,
                new[]
                {
                    NormalBlob("red_a", BlobColor.Red, 0, 0),
                    NormalBlob("blue_a", BlobColor.Blue, 1, 0)
                },
                new[]
                {
                    NormalTile("tile_a", 0, 0),
                    NormalTile("tile_b", 1, 0)
            }));

            session.SelectBlobAt(new GridPosition(0, 0));
            var second = session.SelectBlobAt(new GridPosition(0, 0));

            Assert.That(second.HasSelection, Is.False);
            Assert.That(second.MoveAttempted, Is.False);
            Assert.That(session.CreateSnapshot().Blobs.Count, Is.EqualTo(2));
        }

        [Test]
        public void RestartRestoresAuthoredInitialStateAndClearsHistory()
        {
            var session = new GameSession(new LevelDefinition(
                "restart",
                1,
                2,
                1,
                new[]
                {
                    NormalBlob("red_a", BlobColor.Red, 0, 0),
                    NormalBlob("blue_a", BlobColor.Blue, 1, 0)
                },
                new[]
                {
                    NormalTile("tile_a", 0, 0),
                    NormalTile("tile_b", 1, 0)
                }));
            session.ExecuteMove(new MoveIntent("red_a", "blue_a"));

            session.Restart();
            var snapshot = session.CreateSnapshot();

            Assert.That(snapshot.Blobs.Count, Is.EqualTo(2));
            Assert.That(snapshot.Blobs.Select(blob => blob.Id), Is.EquivalentTo(new[] { "red_a", "blue_a" }));
            Assert.That(snapshot.IsComplete, Is.False);
            Assert.That(snapshot.MoveCount, Is.EqualTo(0));
            Assert.That(snapshot.Tiles.Count, Is.EqualTo(2));
            Assert.That(snapshot.Tiles.Select(tile => tile.Id), Is.EquivalentTo(new[] { "tile_a", "tile_b" }));
            Assert.That(snapshot.CanUndo, Is.False);
        }

        [Test]
        public void BoardClonePreservesTilesAndBlobs()
        {
            var board = new BoardState(
                2,
                1,
                new[]
                {
                    new BlobState("red_a", BlobType.Normal, BlobColor.Red, BlobSize.Normal, new GridPosition(0, 0))
                },
                new[]
                {
                    new TileState("tile_a", new GridPosition(0, 0), TileType.Normal),
                    new TileState("tile_b", new GridPosition(1, 0), TileType.Normal)
                });

            var clone = board.Clone();

            Assert.That(clone.Blobs.Select(blob => blob.Id), Is.EquivalentTo(new[] { "red_a" }));
            Assert.That(clone.Tiles.Select(tile => tile.Id), Is.EquivalentTo(new[] { "tile_a", "tile_b" }));
        }

        private static NormalBlobDefinition NormalBlob(string id, BlobColor color, int x, int y)
        {
            return new NormalBlobDefinition(id, new GridPosition(x, y), color, BlobSize.Normal);
        }

        private static TileDefinition NormalTile(string id, int x, int y)
        {
            return new NormalTileDefinition(id, new GridPosition(x, y));
        }
    }
}
