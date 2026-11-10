using System;
using System.Collections.Generic;
using Blobs.Core;

namespace Blobs.Application
{
    /// <summary>
    /// Whether a tutorial beat is a playable board or a text-only reaction/preface slide.
    /// </summary>
    public enum TutorialStepKind
    {
        /// <summary>The shared board accepts moves, with optional top/bottom coaching text.</summary>
        Playable,

        /// <summary>
        /// Centered text over the dimmed or hidden board. Used to react to the previous move or
        /// preface the next one.
        /// </summary>
        Reaction
    }

    /// <summary>
    /// The player action that completes a tutorial step.
    /// </summary>
    public enum TutorialAdvanceGate
    {
        /// <summary>A move attempt between two blobs was rejected by the rules.</summary>
        FailedMoveAttempt,

        /// <summary>A move succeeded and its presentation has settled.</summary>
        SuccessfulMove,

        /// <summary>A move completed the board's objective and its presentation has settled.</summary>
        LevelComplete,

        /// <summary>The player tapped to dismiss a reaction slide.</summary>
        TapContinue
    }

    /// <summary>
    /// Unity-free description of one tutorial beat on the shared tutorial board: the pieces it
    /// reveals, the captions and optional ghost swipe that coach it, and the player action that
    /// moves the sequence forward.
    /// </summary>
    public sealed class TutorialStep
    {
        private TutorialStep(
            TutorialStepKind kind,
            string captionTop,
            string captionBottom,
            string captionCenter,
            TutorialAdvanceGate advanceWhen,
            IReadOnlyList<BlobDefinition> revealBlobs,
            IReadOnlyList<GridPosition> revealCells,
            IReadOnlyList<string> removeBlobIds,
            bool showPointer,
            GridPosition coachFrom,
            GridPosition coachTo,
            MoveFailureReason failureFilter)
        {
            Kind = kind;
            CaptionTop = captionTop ?? string.Empty;
            CaptionBottom = captionBottom ?? string.Empty;
            CaptionCenter = captionCenter ?? string.Empty;
            AdvanceWhen = advanceWhen;
            RevealBlobs = revealBlobs ?? Array.Empty<BlobDefinition>();
            RevealCells = revealCells ?? Array.Empty<GridPosition>();
            RemoveBlobIds = removeBlobIds ?? Array.Empty<string>();
            ShowPointer = showPointer;
            CoachFrom = coachFrom;
            CoachTo = coachTo;
            FailureFilter = failureFilter;
        }

        /// <summary>
        /// A playable beat. When the step starts, <paramref name="removeBlobIds"/> leave the shared board
        /// first, then <paramref name="revealBlobs"/> and <paramref name="revealCells"/> join it, so a
        /// reveal may take a cell a removed blob just freed.
        /// </summary>
        public static TutorialStep Playable(
            string captionTop,
            TutorialAdvanceGate advanceWhen,
            string captionBottom = null,
            IReadOnlyList<BlobDefinition> revealBlobs = null,
            IReadOnlyList<GridPosition> revealCells = null,
            bool showPointer = false,
            GridPosition coachFrom = default,
            GridPosition coachTo = default,
            MoveFailureReason failureFilter = MoveFailureReason.None,
            IReadOnlyList<string> removeBlobIds = null)
        {
            if (advanceWhen == TutorialAdvanceGate.TapContinue)
                throw new ArgumentException("Playable steps advance on a move gate.", nameof(advanceWhen));

            return new TutorialStep(
                TutorialStepKind.Playable,
                captionTop,
                captionBottom,
                captionCenter: null,
                advanceWhen,
                revealBlobs,
                revealCells,
                removeBlobIds,
                showPointer,
                coachFrom,
                coachTo,
                failureFilter);
        }

        /// <summary>Centered reaction/preface slide. The board stays loaded underneath.</summary>
        public static TutorialStep Reaction(string captionCenter)
        {
            return new TutorialStep(
                TutorialStepKind.Reaction,
                captionTop: null,
                captionBottom: null,
                captionCenter,
                TutorialAdvanceGate.TapContinue,
                revealBlobs: null,
                revealCells: null,
                removeBlobIds: null,
                showPointer: false,
                coachFrom: default,
                coachTo: default,
                failureFilter: MoveFailureReason.None);
        }

        public TutorialStepKind Kind { get; }
        public bool IsReaction => Kind == TutorialStepKind.Reaction;

        public string CaptionTop { get; }
        public string CaptionBottom { get; }
        public string CaptionCenter { get; }

        public TutorialAdvanceGate AdvanceWhen { get; }

        /// <summary>Blobs that join the board when this step starts.</summary>
        public IReadOnlyList<BlobDefinition> RevealBlobs { get; }

        /// <summary>Board cells that open when this step starts.</summary>
        public IReadOnlyList<GridPosition> RevealCells { get; }

        /// <summary>Blob ids that leave the board when this step starts, before reveals join.</summary>
        public IReadOnlyList<string> RemoveBlobIds { get; }

        public bool HasReveals => RevealBlobs.Count > 0 || RevealCells.Count > 0;

        public bool ShowPointer { get; }
        public GridPosition CoachFrom { get; }
        public GridPosition CoachTo { get; }

        /// <summary>
        /// Restricts <see cref="TutorialAdvanceGate.FailedMoveAttempt"/> to one failure reason.
        /// <see cref="MoveFailureReason.None"/> accepts any rejected attempt.
        /// </summary>
        public MoveFailureReason FailureFilter { get; }
    }

    /// <summary>
    /// A mapped tutorial: the board the session starts on (the authored board minus pieces revealed
    /// by later steps) and the ordered steps that reveal the rest.
    /// </summary>
    public sealed class TutorialDefinition
    {
        public TutorialDefinition(LevelDefinition initialLevel, IReadOnlyList<TutorialStep> steps)
        {
            InitialLevel = initialLevel ?? throw new ArgumentNullException(nameof(initialLevel));
            Steps = steps ?? throw new ArgumentNullException(nameof(steps));
        }

        public LevelDefinition InitialLevel { get; }
        public IReadOnlyList<TutorialStep> Steps { get; }
    }

    /// <summary>
    /// Published when a step becomes active. Lists what left and joined the board so Presentation can
    /// animate the change; Core state already reflects it.
    /// </summary>
    public sealed class TutorialStepChange
    {
        public TutorialStepChange(
            TutorialStep step,
            int index,
            IReadOnlyList<string> revealedBlobIds,
            IReadOnlyList<GridPosition> revealedCells,
            IReadOnlyList<string> removedBlobIds = null)
        {
            Step = step ?? throw new ArgumentNullException(nameof(step));
            Index = index;
            RevealedBlobIds = revealedBlobIds ?? Array.Empty<string>();
            RevealedCells = revealedCells ?? Array.Empty<GridPosition>();
            RemovedBlobIds = removedBlobIds ?? Array.Empty<string>();
        }

        public TutorialStep Step { get; }
        public int Index { get; }
        public IReadOnlyList<string> RevealedBlobIds { get; }
        public IReadOnlyList<GridPosition> RevealedCells { get; }

        /// <summary>Blobs that left Core state; their views are still on screen to animate out.</summary>
        public IReadOnlyList<string> RemovedBlobIds { get; }
    }
}
