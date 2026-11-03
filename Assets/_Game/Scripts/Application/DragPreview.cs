using Blobs.Core;

namespace Blobs.Application
{
    /// <summary>
    /// How the move the drag would commit on release is expected to resolve.
    /// </summary>
    public enum DragVerdict
    {
        /// <summary>Nothing to judge yet, or the failure is not one the player should be warned about.</summary>
        Neutral,
        Valid,
        /// <summary>The move would fail for a player-facing rule reason.</summary>
        Invalid
    }

    /// <summary>
    /// Immutable read model of an in-progress source-to-target drag. The tail is the cell the drag
    /// will commit to on release, already constrained to the source's row or column and to the
    /// cells the source can actually reach.
    /// </summary>
    public sealed class DragPreview
    {
        public static readonly DragPreview Inactive =
            new(false, null, default, default, null, DragVerdict.Neutral, MoveFailureReason.None);

        private DragPreview(
            bool isActive,
            string sourceId,
            GridPosition origin,
            GridPosition tail,
            string targetId,
            DragVerdict verdict,
            MoveFailureReason failureReason)
        {
            IsActive = isActive;
            SourceId = sourceId;
            Origin = origin;
            Tail = tail;
            TargetId = targetId;
            Verdict = verdict;
            FailureReason = failureReason;
        }

        /// <summary>True while a source blob is captured by a drag.</summary>
        public bool IsActive { get; }

        /// <summary>The captured source blob id, or null when inactive.</summary>
        public string SourceId { get; }

        /// <summary>The source blob's cell when the drag began.</summary>
        public GridPosition Origin { get; }

        /// <summary>The cell the drag currently extends to. Equals <see cref="Origin"/> until the pointer leaves it.</summary>
        public GridPosition Tail { get; }

        /// <summary>The blob occupying <see cref="Tail"/>, or null when the tail is empty or is the source.</summary>
        public string TargetId { get; }

        /// <summary>Dry-run outcome of committing the drag at <see cref="Tail"/>.</summary>
        public DragVerdict Verdict { get; }

        /// <summary>The dry-run failure reason, or <see cref="MoveFailureReason.None"/> when the move would succeed or no move applies.</summary>
        public MoveFailureReason FailureReason { get; }

        public static DragPreview Active(
            string sourceId,
            GridPosition origin,
            GridPosition tail,
            string targetId,
            DragVerdict verdict,
            MoveFailureReason failureReason)
        {
            return new DragPreview(true, sourceId, origin, tail, targetId, verdict, failureReason);
        }

        public override string ToString()
        {
            return IsActive
                ? $"DragPreview: SourceId = {SourceId}, Origin = {Origin}, Tail = {Tail}, TargetId = {TargetId}, Verdict = {Verdict}, FailureReason = {FailureReason}"
                : "DragPreview: Inactive";
        }
    }
}
