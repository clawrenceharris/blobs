using System.Linq;
using Blobs.Application;
using Blobs.Core;
using NUnit.Framework;

namespace Blobs.Tests.EditMode
{
    public sealed class MilestoneFiveFlagBlobTests
    {
        [Test]
        public void SelectingFlagAsSourceIsRejectedWithoutChangingSessionState()
        {
            GameSession session = CreateSession(
                "flag_source_selection",
                2,
                1,
                FlagBlob("purple_flag", BlobColor.Purple, 0, 0),
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
        public void ExplicitMoveWithFlagAsSourceIsRejectedWithoutMutatingBoard()
        {
            GameSession session = CreateSession(
                "flag_explicit_source",
                2,
                1,
                FlagBlob("purple_flag", BlobColor.Purple, 0, 0),
                NormalBlob("purple_normal", BlobColor.Purple, 1, 0));

            var flagBlob = session.CurrentState.GetBlob("purple_flag");
            var normalBlob = session.CurrentState.GetBlob("purple_normal");

            MoveResult result = session.ExecuteMove(
                new MoveIntent(flagBlob, normalBlob));
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
        public void DifferentColorSourceCannotMergeIntoFlag()
        {

            GameSession session = CreateSession(
                "flag_color_mismatch",
                2,
                1,
                FlagBlob("purple_flag", BlobColor.Purple, 0, 0),
                NormalBlob("blue_normal", BlobColor.Blue, 1, 0));

            var flagBlob = session.CurrentState.GetBlob("purple_flag");
            var normalBlob = session.CurrentState.GetBlob("blue_normal");

            MoveResult result = session.ExecuteMove(
                new MoveIntent(normalBlob, flagBlob));
            GameSessionSnapshot snapshot = session.CreateSnapshot();

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.FailureReason,
                Is.EqualTo(MoveFailureReason.FlagRequiresMatchingColor));
            Assert.That(result.Effects, Is.Empty);
            Assert.That(snapshot.Board.Blobs.Count, Is.EqualTo(2));
            Assert.That(snapshot.MoveCount, Is.Zero);
        }

        [Test]
        public void MatchingSourceCannotMergeIntoFlagWhileAnotherBlobRemains()
        {

            GameSession session = CreateSession(
                "flag_requires_last_blob",
                3,
                2,
                NormalBlob("purple_normal", BlobColor.Purple, 0, 0),
                FlagBlob("purple_flag", BlobColor.Purple, 2, 0),
                NormalBlob("blue_normal", BlobColor.Blue, 0, 1));

            var normalBlob = session.CurrentState.GetBlob("purple_normal");
            var flagBlob = session.CurrentState.GetBlob("purple_flag");

            MoveResult result = session.ExecuteMove(
                new MoveIntent(normalBlob, flagBlob));
            GameSessionSnapshot snapshot = session.CreateSnapshot();

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.FailureReason,
                Is.EqualTo(MoveFailureReason.FlagRequiresNoOtherBlobs));
            Assert.That(result.Effects, Is.Empty);
            Assert.That(snapshot.Board.Blobs.Count, Is.EqualTo(3));
            Assert.That(snapshot.MoveCount, Is.Zero);
        }

        [Test]
        public void FlagMergeStillRequiresRowOrColumnAlignment()
        {
            GameSession session = CreateSession(
                "flag_requires_alignment",
                2,
                2,
                NormalBlob("purple_normal", BlobColor.Purple, 0, 0),
                FlagBlob("purple_flag", BlobColor.Purple, 1, 1));

            var normal = session.CurrentState.GetBlob("purple_normal");
            var flag = session.CurrentState.GetBlob("purple_flag");

            MoveResult result = session.ExecuteMove(
                new MoveIntent(normal, flag));

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.FailureReason,
                Is.EqualTo(MoveFailureReason.NotAligned));
            Assert.That(session.CreateSnapshot().MoveCount, Is.Zero);
        }

        [Test]
        public void BlobOnPathChainMergesBeforeFinalFlagCapture()
        {
            // Chain merging replaced BlockedPath: the mover merges with every
            // occupant on the path, so a valid intermediate merge followed by a
            // flag capture resolves in a single move.
            GameSession session = CreateSession(
                "flag_chain_merge_path",
                3,
                1,
                NormalBlob("purple_normal", BlobColor.Purple, 0, 0),
                NormalBlob("blue_blocker", BlobColor.Blue, 1, 0),
                FlagBlob("purple_flag", BlobColor.Purple, 2, 0));

            var normalBlob = session.CurrentState.GetBlob("purple_normal");
            var flagBlob = session.CurrentState.GetBlob("purple_flag");

            MoveResult result = session.ExecuteMove(
                new MoveIntent(normalBlob, flagBlob));
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
            Assert.That(snapshot.Board.Blobs.Single().Id, Is.EqualTo("purple_flag"));
            Assert.That(snapshot.MoveCount, Is.EqualTo(1));
            Assert.That(snapshot.IsComplete, Is.True);
        }

        [Test]
        public void InvalidIntermediateMergeRejectsWholeFlagMove()
        {
            // The blocker matches the mover's color, so the intermediate merge is
            // invalid and the whole intent fails atomically.
            GameSession session = CreateSession(
                "flag_invalid_chain_link",
                3,
                1,
                NormalBlob("purple_normal", BlobColor.Purple, 0, 0),
                NormalBlob("purple_blocker", BlobColor.Purple, 1, 0),
                FlagBlob("purple_flag", BlobColor.Purple, 2, 0));

            var normalBlob = session.CurrentState.GetBlob("purple_normal");
            var flagBlob = session.CurrentState.GetBlob("purple_flag");

            MoveResult result = session.ExecuteMove(
                new MoveIntent(normalBlob, flagBlob));

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.FailureReason,
                Is.EqualTo(MoveFailureReason.NormalMergeRequiresDifferentColors));
            Assert.That(session.CreateSnapshot().Board.Blobs.Count, Is.EqualTo(3));
            Assert.That(session.CreateSnapshot().MoveCount, Is.Zero);
        }

        [Test]
        public void LastMatchingSourceMergesIntoFlagAndIsConsumed()
        {
            GridPosition sourcePosition = new GridPosition(0, 0);
            GridPosition flagPosition = new GridPosition(2, 0);
            GameSession session = CreateSession(
                "flag_success",
                3,
                1,
                NormalBlob("purple_normal", BlobColor.Purple, sourcePosition.X, sourcePosition.Y),
                FlagBlob("purple_flag", BlobColor.Purple, flagPosition.X, flagPosition.Y));

            var normalBlob = session.CurrentState.GetBlob("purple_normal");
            var flagBlob = session.CurrentState.GetBlob("purple_flag");

            MoveResult result = session.ExecuteMove(
                new MoveIntent(normalBlob, flagBlob));
            GameSessionSnapshot snapshot = session.CreateSnapshot();

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.FailureReason, Is.EqualTo(MoveFailureReason.None));
            // Per-tile timeline: traverse 0,0 -> 1,0, then the capture beat.
            Assert.That(result.Effects.Count, Is.EqualTo(2));

            MergeEffect effect = result.Effects[1] as MergeEffect;

            Assert.That(effect, Is.Not.Null);
            Assert.That(effect.MovingBlobId, Is.EqualTo("purple_normal"));
            Assert.That(effect.TargetBlobId, Is.EqualTo("purple_flag"));
            Assert.That(effect.From, Is.EqualTo(new GridPosition(1, 0)));
            Assert.That(effect.To, Is.EqualTo(flagPosition));

            Assert.That(snapshot.Board.Blobs.Count, Is.EqualTo(1));
            Assert.That(snapshot.Board.Blobs.Single().Id, Is.EqualTo("purple_flag"));
            Assert.That(snapshot.Board.Blobs.Single().Type, Is.EqualTo(BlobType.Flag));
            Assert.That(snapshot.Board.Blobs.Single().Position, Is.EqualTo(flagPosition));
            Assert.That(snapshot.MoveCount, Is.EqualTo(1));
            Assert.That(snapshot.IsComplete, Is.True);
        }

        [Test]
        public void NormalMergeCanClearOtherBlobBeforeFinalFlagMerge()
        {
            GameSession session = CreateSession(
                "flag_full_sequence",
                3,
                1,
                NormalBlob("purple_normal", BlobColor.Purple, 0, 0),
                NormalBlob("blue_normal", BlobColor.Blue, 1, 0),
                FlagBlob("purple_flag", BlobColor.Purple, 2, 0));

            var purpleBlob = session.CurrentState.GetBlob("purple_normal");
            var blueBlob = session.CurrentState.GetBlob("blue_normal");
            var flagBlob = session.CurrentState.GetBlob("purple_flag");

            MoveResult normalMerge = session.ExecuteMove(
                new MoveIntent(purpleBlob, blueBlob));
            MoveResult flagMerge = session.ExecuteMove(
                new MoveIntent(purpleBlob, flagBlob));
            GameSessionSnapshot snapshot = session.CreateSnapshot();

            Assert.That(normalMerge.Succeeded, Is.True);
            Assert.That(flagMerge.Succeeded, Is.True);
            Assert.That(flagMerge.Effects.Single(), Is.TypeOf<MergeEffect>());
            Assert.That(snapshot.Board.Blobs.Count, Is.EqualTo(1));
            Assert.That(snapshot.Board.Blobs.Single().Id, Is.EqualTo("purple_flag"));
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

        private static FlagBlobDefinition FlagBlob(
            string id,
            BlobColor color,
            int x,
            int y)
        {
            return new FlagBlobDefinition(
                id,
                new GridPosition(x, y),
                color);
        }
    }
}
