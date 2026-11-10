using System.Linq;
using Blobs.Application;
using Blobs.Core;
using NUnit.Framework;

namespace Blobs.Tests.EditMode
{
    public sealed class NestMechanicTests
    {
        [Test]
        public void SelectingNestAsSourceIsRejectedWithoutChangingSessionState()
        {
            GameSession session = CreateSession(
                "nest_source_selection",
                2,
                1,
                NestBlob("purple_nest", BlobColor.Purple, 0, 0),
                NormalBlob("purple_normal", BlobColor.Purple, 1, 0));

            BlobSelectionResult result =
                session.SelectBlobAt(new GridPosition(0, 0));
            GameSessionSnapshot snapshot = session.CreateSnapshot();

            Assert.That(result.HasSelection, Is.False);
            Assert.That(result.MoveAttempted, Is.False);
            Assert.That(result.MoveResult, Is.Not.Null);
            Assert.That(
                result.MoveResult.FailureReason,
                Is.EqualTo(MoveFailureReason.SourceCannotMove));
            Assert.That(session.SelectedBlobId, Is.Null);
            Assert.That(snapshot.Board.Blobs.Count, Is.EqualTo(2));
            Assert.That(snapshot.MoveCount, Is.Zero);
            Assert.That(snapshot.IsComplete, Is.False);
        }

        [Test]
        public void ExplicitMoveWithNestAsSourceIsRejectedWithoutMutatingBoard()
        {
            GameSession session = CreateSession(
                "nest_explicit_source",
                2,
                1,
                NestBlob("purple_nest", BlobColor.Purple, 0, 0),
                NormalBlob("purple_normal", BlobColor.Purple, 1, 0));

            var nest = session.CurrentState.GetBlob("purple_nest");
            var normalBlob = session.CurrentState.GetBlob("purple_normal");

            MoveResult result = session.ExecuteMove(
                new MoveIntent(nest, normalBlob));
            GameSessionSnapshot snapshot = session.CreateSnapshot();

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.FailureReason,
                Is.EqualTo(MoveFailureReason.SourceCannotMove));
            Assert.That(result.Effects, Is.Empty);
            Assert.That(snapshot.Board.Blobs.Count, Is.EqualTo(2));
            Assert.That(snapshot.MoveCount, Is.Zero);
        }

        [Test]
        public void DifferentColorSourceCannotMergeIntoNest()
        {

            GameSession session = CreateSession(
                "nest_color_mismatch",
                2,
                1,
                NestBlob("purple_nest", BlobColor.Purple, 0, 0),
                NormalBlob("blue_normal", BlobColor.Blue, 1, 0));

            var nest = session.CurrentState.GetBlob("purple_nest");
            var normalBlob = session.CurrentState.GetBlob("blue_normal");

            MoveResult result = session.ExecuteMove(
                new MoveIntent(normalBlob, nest));
            GameSessionSnapshot snapshot = session.CreateSnapshot();

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.FailureReason,
                Is.EqualTo(MoveFailureReason.NestRequiresMatchingColor));
            Assert.That(result.Effects, Is.Empty);
            Assert.That(snapshot.Board.Blobs.Count, Is.EqualTo(2));
            Assert.That(snapshot.MoveCount, Is.Zero);
        }

        [Test]
        public void OccupiedNestRejectsASecondMatchingSource()
        {
            GameSession session = CreateSession(
                "nest_already_occupied",
                3,
                1,
                NormalBlob("first", BlobColor.Purple, 0, 0),
                NestBlob("purple_nest", BlobColor.Purple, 1, 0),
                NormalBlob("second", BlobColor.Purple, 2, 0));

            MoveResult first = session.ExecuteMove(new MoveIntent(
                session.CurrentState.GetBlob("first"),
                session.CurrentState.GetBlob("purple_nest")));
            Assert.That(first.Succeeded, Is.True);
            Assert.That(session.CurrentState.GetBlob("purple_nest").HasOccupant, Is.True);

            MoveResult second = session.ExecuteMove(new MoveIntent(
                session.CurrentState.GetBlob("second"),
                session.CurrentState.GetBlob("purple_nest")));
            GameSessionSnapshot snapshot = session.CreateSnapshot();

            Assert.That(second.Succeeded, Is.False);
            Assert.That(
                second.FailureReason,
                Is.EqualTo(MoveFailureReason.NestAlreadyOccupied));
            Assert.That(second.Effects, Is.Empty);
            Assert.That(snapshot.Board.GetBlob("second"), Is.Not.Null);
            Assert.That(snapshot.Board.Blobs.Count, Is.EqualTo(2));
        }

        [Test]
        public void NestMergeStillRequiresRowOrColumnAlignment()
        {
            GameSession session = CreateSession(
                "nest_requires_alignment",
                2,
                2,
                NormalBlob("purple_normal", BlobColor.Purple, 0, 0),
                NestBlob("purple_nest", BlobColor.Purple, 1, 1));

            var normal = session.CurrentState.GetBlob("purple_normal");
            var nest = session.CurrentState.GetBlob("purple_nest");

            MoveResult result = session.ExecuteMove(
                new MoveIntent(normal, nest));

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.FailureReason,
                Is.EqualTo(MoveFailureReason.NotAligned));
            Assert.That(session.CreateSnapshot().MoveCount, Is.Zero);
        }

        [Test]
        public void BlobOnPathChainMergesBeforeFinalNestCapture()
        {
            // Chain merging replaced BlockedPath: the mover merges with every
            // occupant on the path, so a valid intermediate merge followed by a
            // nest merge resolves in a single move.
            GameSession session = CreateSession(
                "nest_chain_merge_path",
                3,
                1,
                NormalBlob("purple_normal", BlobColor.Purple, 0, 0),
                NormalBlob("blue_blocker", BlobColor.Blue, 1, 0),
                NestBlob("purple_nest", BlobColor.Purple, 2, 0));

            var normalBlob = session.CurrentState.GetBlob("purple_normal");
            var nest = session.CurrentState.GetBlob("purple_nest");

            MoveResult result = session.ExecuteMove(
                new MoveIntent(normalBlob, nest));
            GameSessionSnapshot snapshot = session.CreateSnapshot();

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Steps.Count, Is.EqualTo(2));
            Assert.That(
                result.Steps[0].Kind,
                Is.EqualTo(MoveStepKind.Merge));
            Assert.That(
                result.Steps[1].Kind,
                Is.EqualTo(MoveStepKind.Merge));
            Assert.That(snapshot.Board.Blobs.Count, Is.EqualTo(1));
            Assert.That(snapshot.Board.Blobs.Single().Id, Is.EqualTo("purple_nest"));
            Assert.That(snapshot.MoveCount, Is.EqualTo(1));
            Assert.That(snapshot.IsComplete, Is.True);
        }

        [Test]
        public void InvalidIntermediateMergeRejectsWholeNestMove()
        {
            // The blocker matches the mover's color, so the intermediate merge is
            // invalid and the whole intent fails atomically.
            GameSession session = CreateSession(
                "nest_invalid_chain_link",
                3,
                1,
                NormalBlob("purple_normal", BlobColor.Purple, 0, 0),
                NormalBlob("purple_blocker", BlobColor.Purple, 1, 0),
                NestBlob("purple_nest", BlobColor.Purple, 2, 0));

            var normalBlob = session.CurrentState.GetBlob("purple_normal");
            var nest = session.CurrentState.GetBlob("purple_nest");

            MoveResult result = session.ExecuteMove(
                new MoveIntent(normalBlob, nest));

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.FailureReason,
                Is.EqualTo(MoveFailureReason.NormalMergeRequiresDifferentColors));
            Assert.That(session.CreateSnapshot().Board.Blobs.Count, Is.EqualTo(3));
            Assert.That(session.CreateSnapshot().MoveCount, Is.Zero);
        }

        [Test]
        public void LastMatchingSourceMergesIntoNestAndIsConsumed()
        {
            GridPosition sourcePosition = new GridPosition(0, 0);
            GridPosition nestPosition = new GridPosition(2, 0);
            GameSession session = CreateSession(
                "nest_success",
                3,
                1,
                NormalBlob("purple_normal", BlobColor.Purple, sourcePosition.X, sourcePosition.Y),
                NestBlob("purple_nest", BlobColor.Purple, nestPosition.X, nestPosition.Y));

            var normalBlob = session.CurrentState.GetBlob("purple_normal");
            var nest = session.CurrentState.GetBlob("purple_nest");

            MoveResult result = session.ExecuteMove(
                new MoveIntent(normalBlob, nest));
            GameSessionSnapshot snapshot = session.CreateSnapshot();

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.FailureReason, Is.EqualTo(MoveFailureReason.None));
            // Per-tile timeline: traverse 0,0 -> 1,0, then the capture beat.
            Assert.That(result.Effects.Count, Is.EqualTo(2));

            var effect = result.Effects[1] as MergeIntoNestEffect;

            Assert.That(effect, Is.Not.Null);
            Assert.That(effect.SourceId, Is.EqualTo("purple_normal"));
            Assert.That(effect.NestId, Is.EqualTo("purple_nest"));
            Assert.That(effect.From, Is.EqualTo(new GridPosition(1, 0)));
            Assert.That(effect.To, Is.EqualTo(nestPosition));

            Assert.That(snapshot.Board.Blobs.Count, Is.EqualTo(1));
            BlobState remaining = snapshot.Board.Blobs.Single();
            Assert.That(remaining.Id, Is.EqualTo("purple_nest"));
            Assert.That(remaining.Type, Is.EqualTo(BlobType.Nest));
            Assert.That(remaining.Position, Is.EqualTo(nestPosition));
            Assert.That(remaining.HasOccupant, Is.True);
            Assert.That(remaining.Components.Occupant?.OccupantId, Is.EqualTo("purple_normal"));
            Assert.That(snapshot.MoveCount, Is.EqualTo(1));
            Assert.That(snapshot.IsComplete, Is.True);
        }

        [Test]
        public void NormalMergeCanClearOtherBlobBeforeFinalNestMerge()
        {
            GameSession session = CreateSession(
                "nest_full_sequence",
                3,
                1,
                NormalBlob("purple_normal", BlobColor.Purple, 0, 0),
                NormalBlob("blue_normal", BlobColor.Blue, 1, 0),
                NestBlob("purple_nest", BlobColor.Purple, 2, 0));

            var purpleBlob = session.CurrentState.GetBlob("purple_normal");
            var blueBlob = session.CurrentState.GetBlob("blue_normal");
            var nest = session.CurrentState.GetBlob("purple_nest");

            MoveResult normalMerge = session.ExecuteMove(
                new MoveIntent(purpleBlob, blueBlob));
            MoveResult nestMerge = session.ExecuteMove(
                new MoveIntent(session.CurrentState.GetBlob("purple_normal"), nest));
            GameSessionSnapshot snapshot = session.CreateSnapshot();

            Assert.That(normalMerge.Succeeded, Is.True);
            Assert.That(nestMerge.Succeeded, Is.True);
            Assert.That(nestMerge.Effects.Single(), Is.TypeOf<MergeIntoNestEffect>());
            Assert.That(snapshot.Board.Blobs.Count, Is.EqualTo(1));
            Assert.That(snapshot.Board.Blobs.Single().Id, Is.EqualTo("purple_nest"));
            Assert.That(snapshot.Board.Blobs.Single().HasOccupant, Is.True);
            Assert.That(snapshot.MoveCount, Is.EqualTo(2));
            Assert.That(snapshot.IsComplete, Is.True);
        }

        private static GameSession CreateSession(
            string levelId,
            int width,
            int height,
            params BlobDefinition[] blobs)
        {
            return new GameSession(new LevelDefinition(
                levelId,
                LevelDefinition.CurrentSchemaVersion,
                width,
                height,
                blobs,
                new TileDefinition[0]));
        }

        private static NormalBlobDefinition NormalBlob(
            string id,
            BlobColor color,
            int x,
            int y)
        {
            return new NormalBlobDefinition(
                id,
                new GridPosition(x, y),
                color);
        }

        private static NestBlobDefinition NestBlob(
            string id,
            BlobColor color,
            int x,
            int y)
        {
            return new NestBlobDefinition(
                id,
                new GridPosition(x, y),
                color);
        }
    }
}
