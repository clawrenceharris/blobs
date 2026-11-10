using System;
using System.Collections.Generic;
using Blobs.Core;

namespace Blobs.Application
{
    /// <summary>
    /// Session surface the tutorial needs: add staged pieces to the live board and observe rejected
    /// move attempts, which are not otherwise published as events.
    /// </summary>
    public interface ITutorialSession : IGameplayState
    {
        /// <summary>
        /// Removes <paramref name="removeBlobIds"/>, opens <paramref name="cells"/>, and adds
        /// <paramref name="blobs"/> to the live board, then makes the result the new Restart baseline
        /// and clears Undo history. Missing removals and blobs whose cell is still occupied are skipped.
        /// Returns the ids that were added.
        /// </summary>
        IReadOnlyList<string> ApplyStage(
            IReadOnlyList<BlobDefinition> blobs,
            IReadOnlyList<GridPosition> cells,
            IReadOnlyList<string> removeBlobIds = null);

        /// <summary>
        /// Restricts which blobs may start a move. Empty or null restores normal selection.
        /// </summary>
        void SetAllowedSourceBlobIds(IReadOnlyList<string> blobIds);

        /// <summary>
        /// Raised when a move between two blobs is rejected by the rules, whether it came from a
        /// tap or a swipe release.
        /// </summary>
        event Action<MoveResult> MoveRejected;
    }

    /// <summary>
    /// Walks an ordered list of <see cref="TutorialStep"/> beats on one shared board. Playable steps
    /// reveal their pieces and advance when their move gate is met. Each step start is a new
    /// Restart baseline and clears Undo history. Reaction slides advance when
    /// <see cref="Continue"/> is called. Completing the last step ends the sequence. Presentation
    /// only renders steps; it never decides when the tutorial moves forward.
    /// </summary>
    /// <remarks>
    /// The session must be created from <see cref="TutorialDefinition.InitialLevel"/>, which already
    /// contains the first step's reveals. Construct the sequencer before the board presenter
    /// subscribes to the session so move resolution reaches the sequencer before a synchronous
    /// presentation settle does.
    /// </remarks>
    public sealed class TutorialSequencer : IDisposable
    {
        private readonly IReadOnlyList<TutorialStep> _steps;
        private readonly ITutorialSession _session;
        private readonly IGameplayPresentationStatus _presentation;
        private readonly Action<Action> _defer;
        private bool _started;
        private bool _disposed;
        private bool _awaitingSettle;
        private bool _advanceScheduled;
        private int _generation;

        /// <param name="steps">Ordered, non-empty tutorial beats. The first must be playable.</param>
        /// <param name="session">Session to observe and to add each step's reveals to.</param>
        /// <param name="presentation">
        /// Optional. When supplied, success gates wait for the move's presentation to settle so
        /// the next step never cuts off a merge animation.
        /// </param>
        /// <param name="defer">
        /// Optional. Runs an advance later (for example next frame) so the board never changes from
        /// inside another presenter's event handler. Null advances immediately.
        /// </param>
        public TutorialSequencer(
            IReadOnlyList<TutorialStep> steps,
            ITutorialSession session,
            IGameplayPresentationStatus presentation = null,
            Action<Action> defer = null)
        {
            if (steps == null || steps.Count == 0)
                throw new ArgumentException("A tutorial needs at least one step.", nameof(steps));
            if (steps[0].IsReaction)
                throw new ArgumentException("A tutorial must start with a playable step.", nameof(steps));

            _steps = steps;
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _presentation = presentation;
            _defer = defer;

            _session.MoveRejected += HandleMoveRejected;
            _session.MoveResolved += HandleMoveResolved;
            _session.StateRestored += HandleStateRestored;
            if (_presentation != null)
                _presentation.PresentationSettled += HandlePresentationSettled;
        }

        public int StepCount => _steps.Count;
        public int StepIndex { get; private set; }
        public bool IsCompleted { get; private set; }
        public TutorialStep CurrentStep => IsCompleted ? null : _steps[StepIndex];

        /// <summary>True while a reaction slide is waiting for the player to tap through.</summary>
        public bool AwaitingContinue =>
            IsRunning && CurrentStep.AdvanceWhen == TutorialAdvanceGate.TapContinue;

        /// <summary>Raised when a step becomes active, including the first.</summary>
        public event Action<TutorialStepChange> StepChanged;

        /// <summary>Raised once, after the last step's gate is met.</summary>
        public event Action SequenceCompleted;

        /// <summary>
        /// Activates the first step. Its reveals are already on the initial board, so they are only
        /// announced for the entrance animation.
        /// </summary>
        public void Start()
        {
            if (_started || _disposed)
                return;

            _started = true;
            TutorialStep first = _steps[0];
            ApplySourceFilter(first);
            var ids = new List<string>(first.RevealBlobs.Count);
            foreach (BlobDefinition blob in first.RevealBlobs)
                ids.Add(blob.Id);
            StepChanged?.Invoke(new TutorialStepChange(first, 0, ids, first.RevealCells));
        }

        /// <summary>
        /// Advances a <see cref="TutorialAdvanceGate.TapContinue"/> step. Ignored for move gates.
        /// </summary>
        public void Continue()
        {
            if (!AwaitingContinue)
                return;

            ScheduleAdvance();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _session.SetAllowedSourceBlobIds(null);
            _session.MoveRejected -= HandleMoveRejected;
            _session.MoveResolved -= HandleMoveResolved;
            _session.StateRestored -= HandleStateRestored;
            if (_presentation != null)
                _presentation.PresentationSettled -= HandlePresentationSettled;
        }

        private bool IsRunning => _started && !_disposed && !IsCompleted;

        private void HandleMoveRejected(MoveResult result)
        {
            if (!IsRunning || result == null)
                return;

            TutorialStep step = CurrentStep;
            if (step.AdvanceWhen != TutorialAdvanceGate.FailedMoveAttempt)
                return;
            if (step.FailureFilter != MoveFailureReason.None &&
                result.FailureReason != step.FailureFilter)
                return;

            ScheduleAdvance();
        }

        private void HandleMoveResolved(MoveResult result)
        {
            if (!IsRunning || result == null || !result.Succeeded)
                return;

            TutorialStep step = CurrentStep;
            bool gateMet = step.AdvanceWhen == TutorialAdvanceGate.SuccessfulMove ||
                (step.AdvanceWhen == TutorialAdvanceGate.LevelComplete && result.IsComplete);
            if (!gateMet)
                return;

            if (_presentation == null)
                ScheduleAdvance();
            else
                _awaitingSettle = true;
        }

        private void HandlePresentationSettled(GameSessionSnapshot snapshot)
        {
            if (!IsRunning || !_awaitingSettle)
                return;

            _awaitingSettle = false;
            ScheduleAdvance();
        }

        /// <summary>
        /// A player Restart invalidates pending advances that were queued against the previous board.
        /// </summary>
        private void HandleStateRestored(GameSessionSnapshot snapshot)
        {
            _generation++;
            _awaitingSettle = false;
            _advanceScheduled = false;
        }

        private void ScheduleAdvance()
        {
            if (_advanceScheduled)
                return;

            _advanceScheduled = true;
            if (_defer == null)
            {
                Advance();
                return;
            }

            int generation = _generation;
            _defer(() =>
            {
                if (generation == _generation)
                    Advance();
            });
        }

        private void Advance()
        {
            _advanceScheduled = false;
            if (!IsRunning)
                return;

            if (StepIndex >= _steps.Count - 1)
            {
                IsCompleted = true;
                _session.SetAllowedSourceBlobIds(null);
                SequenceCompleted?.Invoke();
                return;
            }

            StepIndex++;
            TutorialStep next = _steps[StepIndex];
            // Every step is a checkpoint, so Undo or Restart on a reaction cannot roll back the move
            // it is reacting to. Reactions have no reveals.
            IReadOnlyList<string> revealed = _session.ApplyStage(
                next.RevealBlobs, next.RevealCells, next.RemoveBlobIds);
            ApplySourceFilter(next);
            StepChanged?.Invoke(new TutorialStepChange(
                next, StepIndex, revealed, next.RevealCells, next.RemoveBlobIds));
        }

        private void ApplySourceFilter(TutorialStep step)
        {
            _session.SetAllowedSourceBlobIds(
                step.IsReaction ? null : step.AllowedSourceBlobIds);
        }
    }
}
