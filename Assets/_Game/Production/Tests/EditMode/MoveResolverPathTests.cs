using System;
using System.Linq;
using Blobs.Core;
using NUnit.Framework;

namespace Blobs.Tests.EditMode
{
    /// <summary>Intended versus reached paths, tested against the agreed game rules.</summary>
    public sealed class MoveResolverPathTests
    {
        private static BlobState Blob(string id, BlobType type, int x, BlobColor color = BlobColor.Red) =>
            new BlobState(id, type, new GridPosition(x, 0)).WithColor(color).WithTrail(BlobColor.Blue);

        private static BoardState Board(BlobState[] blobs, params int[] holes) =>
            new BoardState(8, 1, blobs, Array.Empty<TileState>(),
                holes.Select(x => new GridPosition(x, 0)));

        private static MoveResult Resolve(BoardState board) => new MoveResolver().Resolve(board,
            new MoveIntent(board.GetBlob("source"), board.GetBlob("target")));

        private static string Snapshot(BoardState board) => string.Join(";", board.Blobs.OrderBy(b => b.Id)
            .Select(b => $"{b.Id}:{b.Type}:{b.Position}:{b.Components.Color?.Color}:{b.Components.Trail?.TrailColor}"));

        private static void RejectUnchanged(BoardState board, MoveFailureReason? reason = null)
        {
            string before = Snapshot(board);
            MoveResult result = Resolve(board);
            Assert.That(result.Succeeded, Is.False, "The complete intent must be rejected.");
            if (reason.HasValue) Assert.That(result.FailureReason, Is.EqualTo(reason.Value));
            Assert.That(result.Effects, Is.Empty);
            Assert.That(Snapshot(board), Is.EqualTo(before), "Earlier simulated interactions must not commit.");
        }

        [Test]
        public void AdjacentRockDoesNotIncrementSessionMoveCountButStillPublishesContact()
        {
            var level = new LevelDefinition("contact", LevelDefinition.CurrentSchemaVersion, 3, 1,
                new BlobDefinition[]
                {
                    new NormalBlobDefinition("source", new GridPosition(0, 0), BlobColor.Red),
                    new RockBlobDefinition("target", new GridPosition(1, 0)),
                    new FlagBlobDefinition("flag", new GridPosition(2, 0), BlobColor.Red)
                }, Array.Empty<TileDefinition>());
            var session = new Blobs.Application.GameSession(level);
            int contacts = 0;
            session.MoveResolved += _ => contacts++;
            var result = session.ExecuteMove(new MoveIntent(session.CurrentState.GetBlob("source"),
                session.CurrentState.GetBlob("target")));
            Assert.That(result.Succeeded, Is.True);
            Assert.That(session.MoveCount, Is.Zero);
            Assert.That(contacts, Is.EqualTo(1));
        }

        [TestCase(BlobType.Normal)]
        [TestCase(BlobType.Trail)]
        public void TrailTargetsUseNormalColorMergeRules(BlobType sourceType)
        {
            var board = Board(new[] { Blob("source", sourceType, 0),
                Blob("target", BlobType.Trail, 1, BlobColor.Blue) });
            Assert.That(Resolve(board).Succeeded, Is.True);
            Assert.That(board.GetBlob("source").Type, Is.EqualTo(sourceType));
            Assert.That(board.GetBlob("source").Position.X, Is.EqualTo(1));
            Assert.That(board.GetBlob("target"), Is.Null);
        }

        [Test]
        public void TrailCannotCaptureFlagEvenWhenAdjacentAndMatching()
        {
            RejectUnchanged(Board(new[] { Blob("source", BlobType.Trail, 0),
                Blob("target", BlobType.Flag, 1) }), MoveFailureReason.FlagRequiresNormalSource);
        }

        [Test]
        public void IntermediateRockStopsBeforeItAndIgnoresLaterGap()
        {
            var board = Board(new[] { Blob("source", BlobType.Normal, 0),
                Blob("rock", BlobType.Rock, 3), Blob("target", BlobType.Normal, 6, BlobColor.Blue) }, 4);
            var result = Resolve(board);
            Assert.That(result.Succeeded, Is.True);
            Assert.That(board.GetBlob("source").Position, Is.EqualTo(new GridPosition(2, 0)));
            Assert.That(board.GetBlob("rock").Position.X, Is.EqualTo(3));
            Assert.That(board.GetBlob("target").Position.X, Is.EqualTo(6));
        }

        [Test]
        public void ResultIdentifiesTheRockThatActuallyStoppedTheMove()
        {
            var board = Board(new[] { Blob("source", BlobType.Normal, 0),
                Blob("rock", BlobType.Rock, 3), Blob("target", BlobType.Normal, 6, BlobColor.Blue) });
            var plan = new MoveResolver().Resolve(board,
                new MoveIntent(board.GetBlob("source"), board.GetBlob("target")));
            Assert.That(plan.Succeeded, Is.True);
            Assert.That(plan.TargetBlobId, Is.EqualTo("rock"));
            Assert.That(board.GetBlob("source").Position.X, Is.EqualTo(2));
            Assert.That(board.GetBlob("rock").Position.X, Is.EqualTo(3));
            Assert.That(board.GetBlob("target").Position.X, Is.EqualTo(6));
        }

        [Test]
        public void ValidMergeBeforeRockCommitsButDoesNotReachSelectedTarget()
        {
            var board = Board(new[] { Blob("source", BlobType.Normal, 0),
                Blob("middle", BlobType.Normal, 1, BlobColor.Blue), Blob("rock", BlobType.Rock, 3),
                Blob("target", BlobType.Normal, 6, BlobColor.Red) });
            Assert.That(Resolve(board).Succeeded, Is.True);
            Assert.That(board.GetBlob("middle"), Is.Null);
            Assert.That(board.GetBlob("source").Position.X, Is.EqualTo(2));
            Assert.That(board.GetBlob("target"), Is.Not.Null);
        }

        [Test]
        public void GapAfterValidMergeRejectsWithoutPartialCommit()
        {
            var board = Board(new[] { Blob("source", BlobType.Normal, 0),
                Blob("middle", BlobType.Normal, 1, BlobColor.Blue), Blob("target", BlobType.Normal, 5, BlobColor.Blue) }, 3);
            RejectUnchanged(board, MoveFailureReason.PathBlocked);
        }

        [Test]
        public void SameColorBeforeRockRejectsWithoutPartialCommit()
        {
            var board = Board(new[] { Blob("source", BlobType.Normal, 0),
                Blob("middle", BlobType.Normal, 1, BlobColor.Blue), Blob("blocked", BlobType.Normal, 2),
                Blob("rock", BlobType.Rock, 4), Blob("target", BlobType.Normal, 6, BlobColor.Blue) });
            RejectUnchanged(board, MoveFailureReason.NormalMergeRequiresDifferentColors);
        }

        [Test]
        public void AdjacentRockContactProducesNoBoardEffects()
        {
            var board = Board(new[] { Blob("source", BlobType.Trail, 0), Blob("target", BlobType.Rock, 1) });
            string before = Snapshot(board);
            var result = Resolve(board);
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Effects, Is.Empty);
            Assert.That(Snapshot(board), Is.EqualTo(before));
        }

        [Test]
        public void TrailStopsBeforeRockAndSpawnsOnlyOnDepartedCells()
        {
            var board = Board(new[] { Blob("source", BlobType.Trail, 0), Blob("rock", BlobType.Rock, 2),
                Blob("target", BlobType.Normal, 5, BlobColor.Blue) });
            Assert.That(Resolve(board).Succeeded, Is.True);
            Assert.That(board.GetBlob("source").Position.X, Is.EqualTo(1));
            Assert.That(board.GetBlobAt(new GridPosition(0, 0)).Type, Is.EqualTo(BlobType.Normal));
            Assert.That(board.BlobCount, Is.EqualTo(4));
        }

        [Test]
        public void GhostBeforeGapEndsForwardTravelAndHauntsOriginalStart()
        {
            var board = Board(new[] { Blob("source", BlobType.Normal, 0), Blob("ghost", BlobType.Ghost, 3),
                Blob("target", BlobType.Normal, 6, BlobColor.Blue) }, 4);
            Assert.That(Resolve(board).Succeeded, Is.True);
            Assert.That(board.GetBlob("source"), Is.Null);
            Assert.That(board.GetBlob("ghost").Position.X, Is.EqualTo(0));
            Assert.That(board.GetBlob("target").Position.X, Is.EqualTo(6));
        }

        [Test]
        public void FlagCaptureCanClearOtherBlobsEarlierInTheSameAction()
        {
            var board = Board(new[] { Blob("source", BlobType.Normal, 0),
                Blob("middle", BlobType.Normal, 1, BlobColor.Blue), Blob("target", BlobType.Flag, 3) });
            var result = Resolve(board);
            Assert.That(result.Succeeded, Is.True, result.FailureReason.ToString());
            Assert.That(result.IsComplete, Is.True);
            Assert.That(board.Blobs.Select(b => b.Id), Is.EqualTo(new[] { "target" }));
        }

        [Test]
        public void SelectedFlagCannotBeReplacedByAnIntermediateRock()
        {
            RejectUnchanged(Board(new[] { Blob("source", BlobType.Normal, 0),
                Blob("rock", BlobType.Rock, 2), Blob("target", BlobType.Flag, 5) }));
        }

        [Test]
        public void SelectedFlagCannotBeReplacedByAnIntermediateGhost()
        {
            RejectUnchanged(Board(new[] { Blob("source", BlobType.Normal, 0),
                Blob("ghost", BlobType.Ghost, 2), Blob("target", BlobType.Flag, 5) }));
        }

        [Test]
        public void IntermediateFlagRejectsEvenWhenItsColorMatches()
        {
            RejectUnchanged(Board(new[] { Blob("source", BlobType.Normal, 0),
                Blob("flag", BlobType.Flag, 2), Blob("target", BlobType.Rock, 5) }));
        }
    }
}
