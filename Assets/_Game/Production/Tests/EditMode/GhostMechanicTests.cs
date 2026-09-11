using System;
using System.Linq;
using Blobs.Core;
using NUnit.Framework;

namespace Blobs.Tests.EditMode
{
    public sealed class GhostMechanicTests
    {
        private static BlobState Source(bool trail = false) => new BlobState("source",
            trail ? BlobType.Trail : BlobType.Normal, new GridPosition(0, 0))
            .WithColor(BlobColor.Red).WithTrail(BlobColor.Blue);
        private static BlobState Ghost(int x = 3) => new("ghost", BlobType.Ghost, new GridPosition(x, 0));
        private static BoardState Board(bool trail = false, params int[] graves) => new(4, 1,
            new[] { Source(trail), Ghost() }, graves.Select(x =>
                new TileState("grave-" + x, new GridPosition(x, 0), TileType.Grave)));
        private static MoveResult Move(BoardState board) => new MoveResolver().Resolve(board,
            new MoveIntent(board.GetBlob("source"), board.GetBlob("ghost")));

        [Test]
        public void GhostCannotBeSourceAndCountsTowardObjective()
        {
            BoardState board = Board();
            Assert.That(new MoveResolver().ValidateSourceSelection(board, "ghost"),
                Is.EqualTo(MoveFailureReason.SourceCannotMove));
            board.RemoveBlob("source");
            Assert.That(ObjectiveEvaluator.IsComplete(board), Is.False);
        }

        [Test]
        public void EmptyReturnPathPreservesGhostIdentityAndConsumesSource()
        {
            BoardState board = Board();
            MoveResult result = Move(board);
            Assert.That(result.Succeeded, Is.True);
            Assert.That(board.GetBlob("source"), Is.Null);
            Assert.That(board.GetBlob("ghost").Position, Is.EqualTo(new GridPosition(0, 0)));
            Assert.That(board.Blobs.Count(), Is.EqualTo(1));
            var path = result.Effects.OfType<GhostHauntEffect>().Single();
            CollectionAssert.AreEqual(new[] { 2, 1, 0 }, path.Path.Select(x => x.X));
            Assert.That(path.LandingBlobId, Is.Null);
            Assert.That(result.IsComplete, Is.False);
        }

        [TestCase(2)]
        [TestCase(1)]
        [TestCase(0)]
        public void GraveCrossingIncludesGraveAndClearsGhost(int grave)
        {
            BoardState board = Board(false, grave);
            MoveResult result = Move(board);
            Assert.That(result.Succeeded, Is.True);
            Assert.That(board.Blobs, Is.Empty);
            var rest = result.Effects.OfType<GhostRestEffect>().Single();
            Assert.That(rest.RestDestination, Is.EqualTo(new GridPosition(grave, 0)));
            Assert.That(result.Effects.OfType<GhostHauntEffect>(), Is.Empty);
            Assert.That(board.GetTileAt(rest.RestDestination).Type, Is.EqualTo(TileType.Grave));
            Assert.That(ObjectiveEvaluator.IsComplete(board), Is.True);
        }

        [Test]
        public void FirstGraveStopsReturn()
        {
            var result = Move(Board(false, 0, 2));
            Assert.That(result.Effects.OfType<GhostRestEffect>().Single().RestDestination.X, Is.EqualTo(2));
        }

        [Test]
        public void ReturnPhasesThroughTrailAndAbsorbsOnlyLandingOccupant()
        {
            BoardState board = Board(true);
            MoveResult result = Move(board);
            Assert.That(result.Succeeded, Is.True);
            var path = result.Effects.OfType<GhostHauntEffect>().Single();
            Assert.That(path.LandingBlobId, Is.Not.Null);
            Assert.That(board.GetBlob(path.LandingBlobId), Is.Null);
            Assert.That(board.GetBlobAt(new GridPosition(0, 0)).Id, Is.EqualTo("ghost"));
            Assert.That(board.GetBlobAt(new GridPosition(1, 0)).Type, Is.EqualTo(BlobType.Normal));
            Assert.That(board.GetBlobAt(new GridPosition(2, 0)).Type, Is.EqualTo(BlobType.Normal));
        }

        [Test]
        public void OccupiedGraveClearsGhostWithoutAbsorbingTrailOccupant()
        {
            BoardState board = Board(true, 2);
            Move(board);
            Assert.That(board.GetBlob("ghost"), Is.Null);
            Assert.That(board.GetBlobAt(new GridPosition(2, 0)), Is.Null);
        }

        [Test]
        public void ChainMergeReturnsToOriginalSourcePosition()
        {
            BoardState board = Board();
            board.AddBlob(new BlobState("middle", BlobType.Normal, new GridPosition(1, 0))
                .WithColor(BlobColor.Blue));
            Assert.That(Move(board).Succeeded, Is.True);
            Assert.That(board.GetBlob("middle"), Is.Null);
            Assert.That(board.GetBlob("ghost").Position, Is.EqualTo(new GridPosition(0, 0)));
        }

        [Test]
        public void InvalidOutboundCollisionLeavesBoardUnchanged()
        {
            BoardState board = Board();
            board.AddBlob(new BlobState("middle", BlobType.Normal, new GridPosition(1, 0))
                .WithColor(BlobColor.Red));
            Assert.That(Move(board).Succeeded, Is.False);
            Assert.That(board.Blobs.Count(), Is.EqualTo(3));
            Assert.That(board.GetBlob("source").Position.X, Is.Zero);
            Assert.That(board.GetBlob("ghost").Position.X, Is.EqualTo(3));
        }

        [Test]
        public void GhostAlreadyOnGraveRestsWhenHauntBegins()
        {
            var board = Board(false, 3);
            var result = Move(board);
            Assert.That(result.Succeeded, Is.True);
            Assert.That(board.GetBlob("ghost"), Is.Null);
            var rest = result.Effects.OfType<GhostRestEffect>().Single();
            Assert.That(rest.RestDestination.X, Is.EqualTo(3));
            Assert.That(rest.Path.Count, Is.EqualTo(1));
        }

        [Test]
        public void EmptyReturnIsRejectedAtConstruction()
        {
            Assert.Throws<ArgumentException>(() => new GhostHauntEffect("ghost", Array.Empty<GridPosition>()));
        }
    }
}
