using System;
using System.Collections.Generic;
using Blobs.Application;
using Blobs.Core;
using NUnit.Framework;

namespace Blobs.Tests.EditMode
{
    public sealed class TutorialSequencerTests
    {
        private static readonly BlobDefinition Snack =
            new NormalBlobDefinition("snack", new GridPosition(2, 0), BlobColor.Blue);

        private static readonly BlobDefinition Nest =
            new NestBlobDefinition("nest", new GridPosition(2, 0), BlobColor.Red);

        [Test]
        public void FailedMoveGateAdvancesAndRevealsTheNextPiecesOnTheSameBoard()
        {
            GameSession session = CreateSession(TwinsLevel());
            var steps = new[]
            {
                Step(TutorialAdvanceGate.FailedMoveAttempt),
                Step(TutorialAdvanceGate.SuccessfulMove, Snack)
            };
            using var sequencer = new TutorialSequencer(steps, session);
            int restored = 0;
            session.StateRestored += _ => restored++;
            TutorialStepChange change = null;
            sequencer.StepChanged += c => change = c;
            sequencer.Start();

            session.SelectBlobAt(new GridPosition(0, 0));
            session.SelectBlobAt(new GridPosition(1, 0));

            Assert.That(sequencer.StepIndex, Is.EqualTo(1));
            Assert.That(restored, Is.Zero, "Reveals add to the live board instead of rebuilding it.");
            Assert.That(change.RevealedBlobIds, Is.EqualTo(new[] { "snack" }));
            Assert.That(session.CurrentState.GetBlob("snack"), Is.Not.Null);
            Assert.That(session.SelectedBlobId, Is.Null);
            Assert.That(session.MoveCount, Is.Zero);

            session.SelectBlobAt(new GridPosition(1, 0));
            Assert.That(session.SelectBlobAt(new GridPosition(2, 0)).MoveResult.Succeeded, Is.True);
        }

        [Test]
        public void FailedMoveGateHonorsTheFailureFilter()
        {
            GameSession session = CreateSession(TwinsLevel());
            var steps = new[]
            {
                Step(TutorialAdvanceGate.FailedMoveAttempt, MoveFailureReason.NestRequiresMatchingColor),
                Step(TutorialAdvanceGate.SuccessfulMove, Snack)
            };
            using var sequencer = new TutorialSequencer(steps, session);
            sequencer.Start();

            session.SelectBlobAt(new GridPosition(0, 0));
            session.SelectBlobAt(new GridPosition(1, 0));

            Assert.That(sequencer.StepIndex, Is.Zero);
            Assert.That(session.CurrentState.GetBlob("snack"), Is.Null, "Hidden pieces stay off the board.");
        }

        [Test]
        public void SuccessfulMoveGateIgnoresFailedAttempts()
        {
            GameSession session = CreateSession(TwinsLevel());
            var steps = new[]
            {
                Step(TutorialAdvanceGate.SuccessfulMove),
                Step(TutorialAdvanceGate.SuccessfulMove, Snack)
            };
            using var sequencer = new TutorialSequencer(steps, session);
            sequencer.Start();

            session.SelectBlobAt(new GridPosition(0, 0));
            session.SelectBlobAt(new GridPosition(1, 0));

            Assert.That(sequencer.StepIndex, Is.Zero);
            Assert.That(sequencer.IsCompleted, Is.False);
        }

        [Test]
        public void LevelCompleteGateEndsTheSequenceOnceTheRevealedNestIsFilled()
        {
            GameSession session = CreateSession(EatLevel());
            var steps = new[]
            {
                Step(TutorialAdvanceGate.SuccessfulMove),
                Step(TutorialAdvanceGate.LevelComplete, Nest)
            };
            using var sequencer = new TutorialSequencer(steps, session);
            int completed = 0;
            sequencer.SequenceCompleted += () => completed++;
            sequencer.Start();

            session.SelectBlobAt(new GridPosition(0, 0));
            session.SelectBlobAt(new GridPosition(1, 0));
            Assert.That(sequencer.StepIndex, Is.EqualTo(1));
            Assert.That(session.CanUndo, Is.False, "A step start is a new Undo baseline.");
            Assert.That(completed, Is.Zero);

            session.SelectBlobAt(new GridPosition(1, 0));
            session.SelectBlobAt(new GridPosition(2, 0));
            Assert.That(sequencer.IsCompleted, Is.True);
            Assert.That(completed, Is.EqualTo(1));
        }

        [Test]
        public void SuccessGateWaitsForPresentationToSettleBeforeAdvancing()
        {
            GameSession session = CreateSession(EatLevel());
            var presentation = new FakePresentation();
            var steps = new[]
            {
                Step(TutorialAdvanceGate.SuccessfulMove),
                Step(TutorialAdvanceGate.LevelComplete, Nest)
            };
            using var sequencer = new TutorialSequencer(steps, session, presentation);
            sequencer.Start();

            presentation.Settle();
            Assert.That(sequencer.StepIndex, Is.Zero, "A settle without a successful move must not advance.");

            session.SelectBlobAt(new GridPosition(0, 0));
            session.SelectBlobAt(new GridPosition(1, 0));
            Assert.That(sequencer.StepIndex, Is.Zero, "The merge is still presenting.");
            Assert.That(session.CurrentState.GetBlob("nest"), Is.Null);

            presentation.Settle();
            Assert.That(sequencer.StepIndex, Is.EqualTo(1));
            Assert.That(session.CurrentState.GetBlob("nest"), Is.Not.Null);
        }

        [Test]
        public void DeferredAdvanceIsDroppedWhenTheBoardIsRestartedFirst()
        {
            GameSession session = CreateSession(TwinsLevel());
            var queued = new List<Action>();
            var steps = new[]
            {
                Step(TutorialAdvanceGate.FailedMoveAttempt),
                Step(TutorialAdvanceGate.SuccessfulMove, Snack)
            };
            using var sequencer = new TutorialSequencer(steps, session, defer: queued.Add);
            sequencer.Start();

            session.SelectBlobAt(new GridPosition(0, 0));
            session.SelectBlobAt(new GridPosition(1, 0));
            Assert.That(queued, Has.Count.EqualTo(1));
            Assert.That(sequencer.StepIndex, Is.Zero, "Advance must wait for the deferred callback.");

            session.Restart();
            queued[0]();
            Assert.That(sequencer.StepIndex, Is.Zero);

            session.SelectBlobAt(new GridPosition(0, 0));
            session.SelectBlobAt(new GridPosition(1, 0));
            Assert.That(queued, Has.Count.EqualTo(2));
            queued[1]();
            Assert.That(sequencer.StepIndex, Is.EqualTo(1));
        }

        [Test]
        public void StartPublishesTheFirstStepOnceWithItsEntranceReveals()
        {
            var source = new NormalBlobDefinition("source", new GridPosition(0, 0), BlobColor.Red);
            var target = new NormalBlobDefinition("target", new GridPosition(1, 0), BlobColor.Blue);
            GameSession session = CreateSession(Level("start", source, target));
            var steps = new[] { Step(TutorialAdvanceGate.SuccessfulMove, source, target) };
            using var sequencer = new TutorialSequencer(steps, session);
            var seen = new List<TutorialStepChange>();
            sequencer.StepChanged += seen.Add;

            sequencer.Start();
            sequencer.Start();

            Assert.That(seen, Has.Count.EqualTo(1));
            Assert.That(seen[0].Index, Is.Zero);
            Assert.That(seen[0].RevealedBlobIds, Is.EqualTo(new[] { "source", "target" }));
        }

        [Test]
        public void RestartReturnsToTheLatestStageInsteadOfTheInitialBoard()
        {
            GameSession session = CreateSession(TwinsLevel());
            session.ApplyStage(new[] { Snack }, Array.Empty<GridPosition>());

            session.SelectBlobAt(new GridPosition(1, 0));
            session.SelectBlobAt(new GridPosition(2, 0));
            Assert.That(session.CurrentState.GetBlob("snack"), Is.Null, "Twin ate the snack.");

            session.Restart();

            Assert.That(session.CurrentState.GetBlob("snack"), Is.Not.Null);
            Assert.That(session.CurrentState.GetBlob("twin").Position, Is.EqualTo(new GridPosition(1, 0)));
            Assert.That(session.CanUndo, Is.False);
        }

        [Test]
        public void RevealedCellsOpenBeforeTheirBlobsJoin()
        {
            var hiddenCell = new GridPosition(3, 0);
            var level = new LevelDefinition(
                "cells", LevelDefinition.CurrentSchemaVersion, 4, 1,
                TwinsLevel().Blobs, Array.Empty<TileDefinition>(), new[] { hiddenCell });
            GameSession session = CreateSession(level);
            var late = new NormalBlobDefinition("late", hiddenCell, BlobColor.Blue);

            IReadOnlyList<string> added = session.ApplyStage(new[] { late }, new[] { hiddenCell });

            Assert.That(added, Is.EqualTo(new[] { "late" }));
            Assert.That(session.CurrentState.EmptyPositions, Has.No.Member(hiddenCell));
            Assert.That(session.CurrentState.GetBlob("late"), Is.Not.Null);
        }

        [Test]
        public void AllowedSourceFilterBlocksSelectingOtherBlobsAsSources()
        {
            GameSession session = CreateSession(TwinsLevel());
            var swap = new NormalBlobDefinition("swap", new GridPosition(1, 0), BlobColor.Blue);
            var steps = new[]
            {
                Step(TutorialAdvanceGate.FailedMoveAttempt),
                TutorialStep.Playable(
                    "Try the red one!",
                    TutorialAdvanceGate.SuccessfulMove,
                    revealBlobs: new[] { swap },
                    removeBlobIds: new[] { "twin" },
                    allowedSourceBlobIds: new[] { "source" })
            };
            using var sequencer = new TutorialSequencer(steps, session);
            sequencer.Start();

            session.SelectBlobAt(new GridPosition(0, 0));
            session.SelectBlobAt(new GridPosition(1, 0));
            Assert.That(sequencer.StepIndex, Is.EqualTo(1));

            BlobSelectionResult blocked = session.SelectBlobAt(new GridPosition(1, 0));
            Assert.That(blocked.HasSelection, Is.False);
            Assert.That(blocked.MoveResult.FailureReason, Is.EqualTo(MoveFailureReason.SourceCannotMove));
            Assert.That(session.SelectedBlobId, Is.Null);
            Assert.That(session.CurrentState.GetBlob("source"), Is.Not.Null);

            session.SelectBlobAt(new GridPosition(0, 0));
            Assert.That(session.SelectBlobAt(new GridPosition(1, 0)).MoveResult.Succeeded, Is.True);
        }

        [Test]
        public void StepCanSwapABlobOutForARevealOnTheSameCell()
        {
            GameSession session = CreateSession(TwinsLevel());
            var swap = new NormalBlobDefinition("swap", new GridPosition(1, 0), BlobColor.Blue);
            var steps = new[]
            {
                Step(TutorialAdvanceGate.FailedMoveAttempt),
                TutorialStep.Playable(
                    "Try this one instead!",
                    TutorialAdvanceGate.SuccessfulMove,
                    revealBlobs: new[] { swap },
                    removeBlobIds: new[] { "twin" })
            };
            using var sequencer = new TutorialSequencer(steps, session);
            TutorialStepChange change = null;
            sequencer.StepChanged += c => change = c;
            sequencer.Start();

            session.SelectBlobAt(new GridPosition(0, 0));
            session.SelectBlobAt(new GridPosition(1, 0));

            Assert.That(session.CurrentState.GetBlob("twin"), Is.Null);
            Assert.That(session.CurrentState.GetBlobAt(new GridPosition(1, 0)).Id, Is.EqualTo("swap"));
            Assert.That(change.RemovedBlobIds, Is.EqualTo(new[] { "twin" }));
            Assert.That(change.RevealedBlobIds, Is.EqualTo(new[] { "swap" }));

            session.SelectBlobAt(new GridPosition(0, 0));
            Assert.That(session.SelectBlobAt(new GridPosition(1, 0)).MoveResult.Succeeded, Is.True);
        }

        [Test]
        public void RevealOntoAnOccupiedCellIsSkipped()
        {
            GameSession session = CreateSession(TwinsLevel());
            var blocked = new NormalBlobDefinition("blocked", new GridPosition(1, 0), BlobColor.Blue);

            IReadOnlyList<string> added = session.ApplyStage(new[] { blocked }, Array.Empty<GridPosition>());

            Assert.That(added, Is.Empty);
            Assert.That(session.CurrentState.GetBlob("blocked"), Is.Null);
        }

        [Test]
        public void ReactionSlideKeepsTheBoardAndAdvancesOnContinue()
        {
            GameSession session = CreateSession(EatLevel());
            var steps = new[]
            {
                Step(TutorialAdvanceGate.SuccessfulMove),
                TutorialStep.Reaction(captionCenter: "Yum! You got the idea."),
                Step(TutorialAdvanceGate.LevelComplete, Nest)
            };
            using var sequencer = new TutorialSequencer(steps, session);
            int restored = 0;
            session.StateRestored += _ => restored++;
            sequencer.Start();

            session.SelectBlobAt(new GridPosition(0, 0));
            session.SelectBlobAt(new GridPosition(1, 0));
            Assert.That(sequencer.StepIndex, Is.EqualTo(1));
            Assert.That(sequencer.CurrentStep.IsReaction, Is.True);
            Assert.That(sequencer.AwaitingContinue, Is.True);
            Assert.That(session.CurrentState.GetBlob("nest"), Is.Null);

            session.Restart();
            Assert.That(session.CurrentState.GetBlob("target"), Is.Null,
                "Restart on a reaction keeps the move it reacts to.");

            sequencer.Continue();
            Assert.That(sequencer.StepIndex, Is.EqualTo(2));
            Assert.That(session.CurrentState.GetBlob("nest"), Is.Not.Null);
            Assert.That(restored, Is.EqualTo(1), "Only the explicit Restart rebuilds the board.");
        }

        [Test]
        public void ContinueIsIgnoredOnPlayableSteps()
        {
            GameSession session = CreateSession(EatLevel());
            var steps = new[]
            {
                Step(TutorialAdvanceGate.SuccessfulMove),
                Step(TutorialAdvanceGate.SuccessfulMove, Nest)
            };
            using var sequencer = new TutorialSequencer(steps, session);
            sequencer.Start();
            sequencer.Continue();
            Assert.That(sequencer.StepIndex, Is.Zero);
        }

        [Test]
        public void PlayableStepExposesTopAndBottomCaptions()
        {
            TutorialStep step = TutorialStep.Playable(
                "Blobsters never eat their own kind.",
                TutorialAdvanceGate.SuccessfulMove,
                captionBottom: "Try red instead!",
                captionCenter: "Optional center");
            Assert.That(step.CaptionTop, Is.EqualTo("Blobsters never eat their own kind."));
            Assert.That(step.CaptionBottom, Is.EqualTo("Try red instead!"));
            Assert.That(step.CaptionCenter, Is.EqualTo("Optional center"));
            Assert.That(step.HasReveals, Is.False);
        }

        [Test]
        public void ReactionStepAcceptsAnyCaptionSlotAndBoardMode()
        {
            TutorialStep step = TutorialStep.Reaction(
                captionTop: "Nice!",
                reactionBoard: TutorialReactionBoard.Hide);
            Assert.That(step.IsReaction, Is.True);
            Assert.That(step.CaptionTop, Is.EqualTo("Nice!"));
            Assert.That(step.CaptionCenter, Is.Empty);
            Assert.That(step.ReactionBoard, Is.EqualTo(TutorialReactionBoard.Hide));
            Assert.That(step.AdvanceWhen, Is.EqualTo(TutorialAdvanceGate.TapContinue));
        }

        private static GameSession CreateSession(LevelDefinition level) => new GameSession(level);

        private static TutorialStep Step(TutorialAdvanceGate gate, params BlobDefinition[] reveals)
        {
            return TutorialStep.Playable("caption", gate, revealBlobs: reveals);
        }

        private static TutorialStep Step(TutorialAdvanceGate gate, MoveFailureReason filter)
        {
            return TutorialStep.Playable("caption", gate, failureFilter: filter);
        }

        /// <summary>Two same-color blobs side by side; the blue snack at (2,0) starts hidden.</summary>
        private static LevelDefinition TwinsLevel()
        {
            return Level("twins",
                new NormalBlobDefinition("source", new GridPosition(0, 0), BlobColor.Red),
                new NormalBlobDefinition("twin", new GridPosition(1, 0), BlobColor.Red));
        }

        /// <summary>Red can eat blue; the red nest at (2,0) starts hidden.</summary>
        private static LevelDefinition EatLevel()
        {
            return Level("eat",
                new NormalBlobDefinition("source", new GridPosition(0, 0), BlobColor.Red),
                new NormalBlobDefinition("target", new GridPosition(1, 0), BlobColor.Blue));
        }

        private static LevelDefinition Level(string id, params BlobDefinition[] blobs)
        {
            return new LevelDefinition(
                id, LevelDefinition.CurrentSchemaVersion, 4, 1, blobs, Array.Empty<TileDefinition>());
        }

        private sealed class FakePresentation : IGameplayPresentationStatus
        {
            public event Action<GameSessionSnapshot> PresentationSettled;
            public GameSessionSnapshot PresentedSnapshot { get; private set; }

            public void Settle()
            {
                PresentedSnapshot = null;
                PresentationSettled?.Invoke(null);
            }
        }
    }
}
