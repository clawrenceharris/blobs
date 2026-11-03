using System;
using System.Linq;
using Blobs.Application;
using Blobs.Core;
using NUnit.Framework;

namespace Blobs.Tests.EditMode
{
    public sealed class GameSessionTests
    {
        [Test]
        public void DragCommitsOnlyOnReleaseAndOnlyOnce()
        {
            var session = CreateSession();
            session.BeginDragAt(new GridPosition(0, 0));
            Assert.That(session.MoveCount, Is.Zero);
            var result = session.EndDragAt(new GridPosition(1, 0));
            Assert.That(result.MoveResult.Succeeded, Is.True);
            Assert.That(session.MoveCount, Is.EqualTo(1));
            Assert.That(session.EndDragAt(new GridPosition(2, 0)).MoveAttempted, Is.False);
            Assert.That(session.MoveCount, Is.EqualTo(1));
        }

        [TestCase(3, 1)]
        [TestCase(0, 3)]
        [TestCase(0, 0)]
        [TestCase(-1, 1)]
        public void DragWithMissingEndpointOrSamePieceDoesNotCommit(int startX, int endX)
        {
            var session = CreateSession();
            session.BeginDragAt(new GridPosition(startX, 0));
            session.EndDragAt(new GridPosition(endX, 0));
            Assert.That(session.MoveCount, Is.Zero);
            Assert.That(session.CanUndo, Is.False);
            Assert.That(session.SelectedBlobId, Is.Null);
        }

        [Test]
        public void InvalidDragDoesNotCommitAndNextDragCanStartFresh()
        {
            var session = CreateSession(BlobColor.Red);
            session.BeginDragAt(new GridPosition(0, 0));
            var result = session.EndDragAt(new GridPosition(1, 0));
            Assert.That(result.MoveResult.Succeeded, Is.False);
            Assert.That(session.MoveCount, Is.Zero);
            Assert.That(session.CurrentState.BlobCount, Is.EqualTo(3));
            Assert.That(session.SelectedBlobId, Is.Null);
            Assert.That(session.BeginDragAt(new GridPosition(1, 0)).SourceId, Is.EqualTo("target"));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CanceledOrRestartedDragCannotCommitOnRelease(bool restart)
        {
            var session = CreateSession();
            session.BeginDragAt(new GridPosition(0, 0));
            if (restart) session.Restart();
            else session.CancelDrag();
            Assert.That(session.EndDragAt(new GridPosition(1, 0)).MoveAttempted, Is.False);
            Assert.That(session.MoveCount, Is.Zero);
        }

        [Test]
        public void DragPreviewExtendsToClampedTailOnlyWhenTheTailChanges()
        {
            var session = CreateSession();
            var previews = new System.Collections.Generic.List<DragPreview>();
            BlobSelectionResult lastSelection = null;
            session.DragPreviewChanged += previews.Add;
            session.BlobSelected += result => lastSelection = result;

            session.BeginDragAt(new GridPosition(0, 0));
            Assert.That(previews.Last().IsActive, Is.True);
            Assert.That(previews.Last().Tail, Is.EqualTo(new GridPosition(0, 0)));

            Assert.That(previews.Last().Verdict, Is.EqualTo(DragVerdict.Neutral));

            session.UpdateDragAt(new GridPosition(1, 0));
            Assert.That(previews.Last().Tail, Is.EqualTo(new GridPosition(1, 0)));
            Assert.That(previews.Last().TargetId, Is.EqualTo("target"));
            Assert.That(previews.Last().Verdict, Is.EqualTo(DragVerdict.Valid));
            Assert.That(lastSelection.TargetId, Is.EqualTo("target"));
            Assert.That(lastSelection.TargetSelected, Is.True);

            int published = previews.Count;
            session.UpdateDragAt(new GridPosition(1, 0));
            Assert.That(previews.Count, Is.EqualTo(published));

            session.UpdateDragAt(new GridPosition(9, 0));
            Assert.That(previews.Last().Tail, Is.EqualTo(new GridPosition(3, 0)));
            Assert.That(previews.Last().TargetId, Is.Null);
            Assert.That(previews.Last().Verdict, Is.EqualTo(DragVerdict.Neutral));
            Assert.That(session.MoveCount, Is.Zero);
        }

        [Test]
        public void DragOntoMismatchedFlagIsInvalidWithoutCommitting()
        {
            var session = CreateLineSession(
                new NormalBlobDefinition("source", new GridPosition(0, 0), BlobColor.Red),
                new FlagBlobDefinition("flag", new GridPosition(1, 0), BlobColor.Blue));
            DragPreview last = null;
            session.DragPreviewChanged += preview => last = preview;

            session.BeginDragAt(new GridPosition(0, 0));
            session.UpdateDragAt(new GridPosition(1, 0));

            Assert.That(last.Verdict, Is.EqualTo(DragVerdict.Invalid));
            Assert.That(last.FailureReason, Is.EqualTo(MoveFailureReason.FlagRequiresMatchingColor));
            Assert.That(session.MoveCount, Is.Zero);
            Assert.That(session.CurrentState.GetBlob("source").Position, Is.EqualTo(new GridPosition(0, 0)));
        }

        [Test]
        public void DragOntoSameColorBlobIsInvalid()
        {
            var session = CreateSession(BlobColor.Red);
            DragPreview last = null;
            BlobSelectionResult lastSelection = null;
            session.DragPreviewChanged += preview => last = preview;
            session.BlobSelected += result => lastSelection = result;

            session.BeginDragAt(new GridPosition(0, 0));
            session.UpdateDragAt(new GridPosition(1, 0));

            Assert.That(last.Verdict, Is.EqualTo(DragVerdict.Invalid));
            Assert.That(last.FailureReason, Is.EqualTo(MoveFailureReason.NormalMergeRequiresDifferentColors));
            Assert.That(lastSelection.SourceId, Is.EqualTo("source"));
            Assert.That(lastSelection.TargetId, Is.EqualTo("target"));
            Assert.That(lastSelection.TargetSelected, Is.False);
            Assert.That(lastSelection.IsSelected("target"), Is.False);
        }

        [Test]
        public void DragPastRockStopsTailOnTheRockAndCommitsContact()
        {
            var session = CreateLineSession(
                new NormalBlobDefinition("source", new GridPosition(0, 0), BlobColor.Red),
                new RockBlobDefinition("rock", new GridPosition(1, 0)),
                new NormalBlobDefinition("beyond", new GridPosition(2, 0), BlobColor.Blue),
                new FlagBlobDefinition("flag", new GridPosition(3, 0), BlobColor.Red));
            DragPreview last = null;
            session.DragPreviewChanged += preview => last = preview;

            session.BeginDragAt(new GridPosition(0, 0));
            session.UpdateDragAt(new GridPosition(2, 0));
            Assert.That(last.Tail, Is.EqualTo(new GridPosition(1, 0)));
            Assert.That(last.TargetId, Is.EqualTo("rock"));
            Assert.That(last.Verdict, Is.EqualTo(DragVerdict.Valid));

            var result = session.EndDragAt(new GridPosition(3, 0));
            Assert.That(result.TargetId, Is.EqualTo("rock"));
            Assert.That(session.CurrentState.GetBlob("beyond"), Is.Not.Null);
        }

        [Test]
        public void DragAcrossGapStopsTailOnTheLastReachableCell()
        {
            var session = new GameSession(new LevelDefinition("drag-gap", LevelDefinition.CurrentSchemaVersion, 4, 1,
                new BlobDefinition[]
                {
                    new NormalBlobDefinition("source", new GridPosition(0, 0), BlobColor.Red),
                    new NormalBlobDefinition("near", new GridPosition(1, 0), BlobColor.Blue),
                    new FlagBlobDefinition("flag", new GridPosition(3, 0), BlobColor.Red)
                }, Array.Empty<TileDefinition>(), new[] { new GridPosition(2, 0) }));
            DragPreview last = null;
            session.DragPreviewChanged += preview => last = preview;

            session.BeginDragAt(new GridPosition(0, 0));
            session.UpdateDragAt(new GridPosition(3, 0));

            Assert.That(last.Tail, Is.EqualTo(new GridPosition(1, 0)));
            Assert.That(last.TargetId, Is.EqualTo("near"));
            Assert.That(last.Verdict, Is.EqualTo(DragVerdict.Valid));
        }

        [Test]
        public void DragThroughFlagTowardBlobBeyondIsInvalid()
        {
            var session = CreateLineSession(
                new NormalBlobDefinition("source", new GridPosition(0, 0), BlobColor.Red),
                new FlagBlobDefinition("flag", new GridPosition(1, 0), BlobColor.Red),
                new NormalBlobDefinition("beyond", new GridPosition(2, 0), BlobColor.Blue));
            DragPreview last = null;
            session.DragPreviewChanged += preview => last = preview;

            session.BeginDragAt(new GridPosition(0, 0));
            session.UpdateDragAt(new GridPosition(2, 0));

            Assert.That(last.Tail, Is.EqualTo(new GridPosition(2, 0)));
            Assert.That(last.Verdict, Is.EqualTo(DragVerdict.Invalid));
            Assert.That(last.FailureReason, Is.EqualTo(MoveFailureReason.FlagCaptureRequired));
        }

        [Test]
        public void DragPreviewsDoNotShiftIdsSpawnedByTheCommittedMove()
        {
            BlobDefinition[] blobs =
            {
                new TrailBlobDefinition("source", new GridPosition(0, 0), BlobColor.Red, BlobColor.Blue),
                new NormalBlobDefinition("target", new GridPosition(3, 0), BlobColor.Green),
                new FlagBlobDefinition("flag", new GridPosition(4, 0), BlobColor.Red)
            };
            var previewed = CreateLineSession(blobs);
            previewed.BeginDragAt(new GridPosition(0, 0));
            previewed.UpdateDragAt(new GridPosition(3, 0));
            previewed.UpdateDragAt(new GridPosition(2, 0));
            previewed.UpdateDragAt(new GridPosition(3, 0));
            Assert.That(previewed.EndDragAt(new GridPosition(3, 0)).MoveResult.Succeeded, Is.True);

            var direct = CreateLineSession(blobs);
            Assert.That(direct.ExecuteMove(Intent(direct, "source", "target")).Succeeded, Is.True);

            Assert.That(previewed.CurrentState.Blobs.Select(b => b.Id),
                Is.EquivalentTo(direct.CurrentState.Blobs.Select(b => b.Id)));
            Assert.That(previewed.CurrentState.Blobs.Count, Is.GreaterThan(3));
        }

        [Test]
        public void DiagonalDragProjectsOntoDominantAxisAndReleaseCommitsToThatTail()
        {
            var session = new GameSession(new LevelDefinition("drag-axis", LevelDefinition.CurrentSchemaVersion, 3, 3,
                new BlobDefinition[]
                {
                    new NormalBlobDefinition("source", new GridPosition(1, 1), BlobColor.Red),
                    new NormalBlobDefinition("target", new GridPosition(2, 1), BlobColor.Blue),
                    new FlagBlobDefinition("flag", new GridPosition(0, 0), BlobColor.Red)
                }, Array.Empty<TileDefinition>()));
            DragPreview last = null;
            session.DragPreviewChanged += preview => last = preview;

            session.BeginDragAt(new GridPosition(1, 1));
            session.UpdateDragAt(new GridPosition(2, 2));
            Assert.That(last.Tail, Is.EqualTo(new GridPosition(2, 1)));

            var result = session.EndDragAt(new GridPosition(2, 2));
            Assert.That(result.MoveResult.Succeeded, Is.True);
            Assert.That(result.TargetId, Is.EqualTo("target"));
            Assert.That(last.IsActive, Is.False);
        }

        [Test]
        public void DragPreviewEndsOnceBeforeTheMoveResolves()
        {
            var session = CreateSession();
            int inactive = 0;
            bool previewEndedBeforeMove = false;
            session.DragPreviewChanged += preview => { if (!preview.IsActive) inactive++; };
            session.MoveResolved += _ => previewEndedBeforeMove = inactive == 1;

            session.BeginDragAt(new GridPosition(0, 0));
            session.UpdateDragAt(new GridPosition(1, 0));
            session.EndDragAt(new GridPosition(1, 0));
            session.CancelDrag();

            Assert.That(previewEndedBeforeMove, Is.True);
            Assert.That(inactive, Is.EqualTo(1));
        }

        [TestCase(0)]
        [TestCase(3)]
        public void SelectingSourceAgainOrEmptyCellClearsSelectionWithoutMoving(int x)
        {
            var session = CreateSession();
            Assert.That(session.SelectBlobAt(new GridPosition(0, 0)).HasSelection, Is.True);
            var result = session.SelectBlobAt(new GridPosition(x, 0));
            Assert.That(result.HasSelection, Is.False);
            Assert.That(result.MoveAttempted, Is.False);
            Assert.That(session.SelectedBlobId, Is.Null);
            Assert.That(session.MoveCount, Is.Zero);
            Assert.That(session.CurrentState.BlobCount, Is.EqualTo(3));
        }

        [Test]
        public void SelectionExecutesOneMoveAndPublishesCommittedState()
        {
            var session = CreateSession();
            int resolved = 0, snapshots = 0;
            session.MoveResolved += result =>
            {
                resolved++;
                Assert.That(result.Succeeded, Is.True);
                Assert.That(session.CurrentState.GetBlob("target"), Is.Null);
                Assert.That(session.MoveCount, Is.EqualTo(1));
            };
            session.SnapshotChanged += snapshot =>
            {
                snapshots++;
                Assert.That(snapshot.MoveCount, Is.EqualTo(1));
                Assert.That(snapshot.Board.GetBlob("source").Position, Is.EqualTo(new GridPosition(1, 0)));
            };
            session.SelectBlobAt(new GridPosition(0, 0));
            var move = session.SelectBlobAt(new GridPosition(1, 0));
            Assert.That(move.MoveAttempted, Is.True);
            Assert.That(move.MoveResult.Succeeded, Is.True);
            Assert.That(session.SelectedBlobId, Is.Null);
            Assert.That(resolved, Is.EqualTo(1));
            Assert.That(snapshots, Is.EqualTo(1));
        }

        [Test]
        public void RejectedMoveClearsSelectionWithoutPublishingACommittedMove()
        {
            var session = CreateSession(BlobColor.Red);
            int resolved = 0, snapshots = 0;
            session.MoveResolved += _ => resolved++;
            session.SnapshotChanged += _ => snapshots++;
            session.SelectBlobAt(new GridPosition(0, 0));
            var result = session.SelectBlobAt(new GridPosition(1, 0));
            Assert.That(result.MoveAttempted, Is.True);
            Assert.That(result.MoveResult.FailureReason, Is.EqualTo(MoveFailureReason.NormalMergeRequiresDifferentColors));
            Assert.That(session.SelectedBlobId, Is.Null);
            Assert.That(session.MoveCount, Is.Zero);
            Assert.That(session.CurrentState.BlobCount, Is.EqualTo(3));
            Assert.That(resolved, Is.Zero);
            Assert.That(snapshots, Is.Zero);
        }

        [Test]
        public void RestartAfterCompletionRestoresAuthoredBoardAndResetsCountAndSelection()
        {
            var session = CreateSession();
            session.SelectBlobAt(new GridPosition(0, 0));
            session.SelectBlobAt(new GridPosition(1, 0));
            session.SelectBlobAt(new GridPosition(1, 0));
            session.SelectBlobAt(new GridPosition(2, 0));
            Assert.That(session.IsComplete, Is.True);
            Assert.That(session.MoveCount, Is.EqualTo(2));
            int restored = 0;
            session.StateRestored += snapshot =>
            {
                restored++;
                Assert.That(snapshot.MoveCount, Is.Zero);
                Assert.That(snapshot.IsComplete, Is.False);
                Assert.That(snapshot.Board.Blobs.Select(b => b.Id), Is.EquivalentTo(new[] { "source", "target", "flag" }));
            };
            session.Restart();
            Assert.That(restored, Is.EqualTo(1));
            Assert.That(session.SelectedBlobId, Is.Null);
            Assert.That(session.CurrentState.GetBlob("source").Position, Is.EqualTo(new GridPosition(0, 0)));
            session.SelectBlobAt(new GridPosition(0, 0));
            session.Restart();
            Assert.That(session.SelectedBlobId, Is.Null);
            Assert.That(session.SelectBlobAt(new GridPosition(0, 0)).HasSelection, Is.True);
        }

        [Test]
        public void SnapshotsRemainIndependentOfLaterMovesAndExternalBoardMutations()
        {
            var session = CreateSession();
            var before = session.CreateSnapshot();
            session.SelectBlobAt(new GridPosition(0, 0));
            session.SelectBlobAt(new GridPosition(1, 0));
            Assert.That(before.Board.BlobCount, Is.EqualTo(3));
            Assert.That(before.Board.GetBlob("source").Position, Is.EqualTo(new GridPosition(0, 0)));
            before.Board.RemoveBlob("flag");
            before.Board.EmptyPositions.Add(new GridPosition(3, 0));
            Assert.That(session.CurrentState.GetBlob("flag"), Is.Not.Null);
            Assert.That(session.CurrentState.EmptyPositions, Is.Empty);
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

        [Test]
        public void TwoBoardChangingMovesUndoTwiceRestoresInitialBoardAndKeepsCount()
        {
            var session = CreateChainSession();
            session.ExecuteMove(Intent(session, "source", "mid"));
            session.ExecuteMove(Intent(session, "source", "far"));
            Assert.That(session.MoveCount, Is.EqualTo(2));
            Assert.That(session.IsComplete, Is.False);
            Assert.That(session.CanUndo, Is.True);

            Assert.That(session.Undo(), Is.True);
            Assert.That(session.MoveCount, Is.EqualTo(2));
            Assert.That(session.CurrentState.GetBlob("far"), Is.Not.Null);
            Assert.That(session.CanUndo, Is.True);

            Assert.That(session.Undo(), Is.True);
            Assert.That(session.MoveCount, Is.EqualTo(2));
            Assert.That(session.CanUndo, Is.False);
            Assert.That(session.CurrentState.GetBlob("source").Position, Is.EqualTo(new GridPosition(0, 0)));
            Assert.That(session.CurrentState.GetBlob("mid"), Is.Not.Null);
            Assert.That(session.CurrentState.GetBlob("far"), Is.Not.Null);
            Assert.That(session.Undo(), Is.False);
        }

        [Test]
        public void NewMoveAfterUndoIncrementsCountAndDiscardsUndoneFuture()
        {
            var session = CreateChainSession();
            session.ExecuteMove(Intent(session, "source", "mid"));
            session.ExecuteMove(Intent(session, "source", "far"));
            session.Undo();
            Assert.That(session.CurrentState.GetBlob("far"), Is.Not.Null);

            var result = session.ExecuteMove(Intent(session, "source", "far"));
            Assert.That(result.Succeeded, Is.True);
            Assert.That(session.MoveCount, Is.EqualTo(3));
            Assert.That(session.CurrentState.GetBlob("far"), Is.Null);
            Assert.That(session.CurrentState.GetBlob("mid"), Is.Null);
            session.Undo();
            Assert.That(session.CurrentState.GetBlob("far"), Is.Not.Null);
            Assert.That(session.CurrentState.GetBlob("mid"), Is.Null);
        }

        [Test]
        public void RejectionAfterValidMoveDoesNotChangeWhichActionUndoRestores()
        {
            var session = CreateChainSession();
            session.ExecuteMove(Intent(session, "source", "mid"));
            Assert.That(session.MoveCount, Is.EqualTo(1));

            var rejected = session.ExecuteMove(Intent(session, "source", "flag"));
            Assert.That(rejected.Succeeded, Is.False);
            Assert.That(session.MoveCount, Is.EqualTo(1));
            Assert.That(session.CurrentState.GetBlob("mid"), Is.Null);

            Assert.That(session.Undo(), Is.True);
            Assert.That(session.MoveCount, Is.EqualTo(1));
            Assert.That(session.CurrentState.GetBlob("mid"), Is.Not.Null);
            Assert.That(session.CurrentState.GetBlob("source").Position, Is.EqualTo(new GridPosition(0, 0)));
        }

        [Test]
        public void UnchangedRockContactAfterValidMoveDoesNotChangeWhichActionUndoRestores()
        {
            var session = new GameSession(new LevelDefinition("undo-contact", LevelDefinition.CurrentSchemaVersion, 4, 1,
                new BlobDefinition[]
                {
                    new NormalBlobDefinition("source", new GridPosition(0, 0), BlobColor.Red),
                    new NormalBlobDefinition("target", new GridPosition(1, 0), BlobColor.Blue),
                    new RockBlobDefinition("rock", new GridPosition(2, 0)),
                    new FlagBlobDefinition("flag", new GridPosition(3, 0), BlobColor.Red)
                }, Array.Empty<TileDefinition>()));
            session.ExecuteMove(Intent(session, "source", "target"));
            Assert.That(session.MoveCount, Is.EqualTo(1));

            var contact = session.ExecuteMove(Intent(session, "source", "rock"));
            Assert.That(contact.Succeeded, Is.True);
            Assert.That(session.MoveCount, Is.EqualTo(1));
            Assert.That(session.CurrentState.GetBlob("target"), Is.Null);

            Assert.That(session.Undo(), Is.True);
            Assert.That(session.CurrentState.GetBlob("target"), Is.Not.Null);
            Assert.That(session.CurrentState.GetBlob("source").Position, Is.EqualTo(new GridPosition(0, 0)));
        }

        [Test]
        public void WinningActionLocksUndoWithoutReducingCount()
        {
            var session = CreateSession();
            session.ExecuteMove(Intent(session, "source", "target"));
            session.ExecuteMove(Intent(session, "source", "flag"));
            Assert.That(session.IsComplete, Is.True);
            Assert.That(session.MoveCount, Is.EqualTo(2));
            Assert.That(session.CanUndo, Is.False);
            Assert.That(session.Undo(), Is.False);
            Assert.That(session.IsComplete, Is.True);
            Assert.That(session.CurrentState.GetBlob("target"), Is.Null);
        }

        [Test]
        public void UndoRestoresConsumedBlobIdentityAndClearsSelection()
        {
            var session = CreateSession();
            session.SelectBlobAt(new GridPosition(0, 0));
            int undone = 0;
            session.UndoResolved += result =>
            {
                undone++;
                Assert.That(result.ForwardAction.Succeeded, Is.True);
                Assert.That(result.RestoredSnapshot.Board.GetBlob("target").Type, Is.EqualTo(BlobType.Normal));
                Assert.That(result.RestorationBlobs.ContainsKey("target"), Is.True);
            };
            session.ExecuteMove(Intent(session, "source", "target"));
            session.SelectBlobAt(new GridPosition(1, 0));
            Assert.That(session.SelectedBlobId, Is.EqualTo("source"));
            Assert.That(session.Undo(), Is.True);
            Assert.That(undone, Is.EqualTo(1));
            Assert.That(session.SelectedBlobId, Is.Null);
            Assert.That(session.CreateSnapshot().CanUndo, Is.False);
        }

        [Test]
        public void UndoRestoresTrailLeftoversConsumedDuringGhostHaunt()
        {
            var session = new GameSession(new LevelDefinition("trail-undo", LevelDefinition.CurrentSchemaVersion, 4, 1,
                new BlobDefinition[]
                {
                    new TrailBlobDefinition("source", new GridPosition(0, 0), BlobColor.Red, BlobColor.Blue),
                    new GhostBlobDefinition("ghost", new GridPosition(2, 0)),
                    new FlagBlobDefinition("flag", new GridPosition(3, 0), BlobColor.Green)
                }, Array.Empty<TileDefinition>()));
            var result = session.ExecuteMove(Intent(session, "source", "ghost"));
            Assert.That(result.Succeeded, Is.True);
            Assert.That(session.CurrentState.GetBlob("source"), Is.Null);
            Assert.That(session.Undo(), Is.True);
            Assert.That(session.CurrentState.GetBlob("source").Type, Is.EqualTo(BlobType.Trail));
            Assert.That(session.CurrentState.GetBlob("source").Position, Is.EqualTo(new GridPosition(0, 0)));
            Assert.That(session.CurrentState.GetBlob("ghost").Position, Is.EqualTo(new GridPosition(2, 0)));
        }

        private static MoveIntent Intent(GameSession session, string sourceId, string targetId)
        {
            return new MoveIntent(session.CurrentState.GetBlob(sourceId), session.CurrentState.GetBlob(targetId));
        }

        private static GameSession CreateLineSession(params BlobDefinition[] blobs)
        {
            return new GameSession(new LevelDefinition("drag-line", LevelDefinition.CurrentSchemaVersion, 5, 1,
                blobs, Array.Empty<TileDefinition>()));
        }

        private static GameSession CreateChainSession()
        {
            return new GameSession(new LevelDefinition("undo-chain", LevelDefinition.CurrentSchemaVersion, 5, 1,
                new BlobDefinition[]
                {
                    new NormalBlobDefinition("source", new GridPosition(0, 0), BlobColor.Red),
                    new NormalBlobDefinition("mid", new GridPosition(1, 0), BlobColor.Blue),
                    new NormalBlobDefinition("far", new GridPosition(2, 0), BlobColor.Green),
                    new FlagBlobDefinition("flag", new GridPosition(3, 0), BlobColor.Red)
                }, Array.Empty<TileDefinition>()));
        }

        private static GameSession CreateSession(BlobColor targetColor = BlobColor.Blue)
        {
            return new GameSession(new LevelDefinition("session-test", LevelDefinition.CurrentSchemaVersion, 4, 1,
                new BlobDefinition[]
                {
                    new NormalBlobDefinition("source", new GridPosition(0, 0), BlobColor.Red),
                    new NormalBlobDefinition("target", new GridPosition(1, 0), targetColor),
                    new FlagBlobDefinition("flag", new GridPosition(2, 0), BlobColor.Red)
                }, Array.Empty<TileDefinition>()));
        }
    }
}
