using System;
using System.Linq;
using Blobs.Application;
using Blobs.Core;
using NUnit.Framework;

namespace Blobs.Tests.EditMode
{
    public sealed class GameSessionTests
    {
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
