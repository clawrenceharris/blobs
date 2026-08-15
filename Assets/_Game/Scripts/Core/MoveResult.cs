using System.Collections.Generic;

namespace Blobs.Core
{
    /// <summary>
    /// Immutable result of resolving a move intent against a board.
    /// Successful results contain the ordered effects that were already applied to Core state.
    /// </summary>
    public sealed class MoveResult
    {
        /// <summary>
        /// Creates a resolved move result.
        /// </summary>
        public MoveResult(
            bool succeeded,
            MoveFailureReason failureReason,
            IReadOnlyList<IBoardEffect> effects,
            bool isComplete,
            IReadOnlyList<MoveStep> steps = null)
        {
            Succeeded = succeeded;
            FailureReason = failureReason;
            Effects = effects ?? new List<IBoardEffect>();
            IsComplete = isComplete;
            Steps = steps ?? new List<MoveStep>();
        }

        /// <summary>
        /// True when the resolver accepted the intent and applied its effects.
        /// </summary>
        public bool Succeeded { get; }

        /// <summary>
        /// Rejection reason for failed moves, or <see cref="MoveFailureReason.None"/> for success.
        /// </summary>
        public MoveFailureReason FailureReason { get; }

        /// <summary>
        /// Ordered effects emitted for the move. Presentation should consume this list in order.
        /// </summary>
        public IReadOnlyList<IBoardEffect> Effects { get; }

        /// <summary>
        /// Completion state after the move has been applied.
        /// </summary>
        public bool IsComplete { get; }

        /// <summary>
        /// Sequential timeline beats for this move. Effects inside one step are
        /// logically simultaneous; steps play in order. Empty for failed moves and
        /// for results produced by callers that only supply flat effects.
        /// </summary>
        public IReadOnlyList<MoveStep> Steps { get; }

        /// <summary>
        /// Creates a failed result with no board effects.
        /// </summary>
        public static MoveResult Failed(MoveFailureReason reason)
        {
            return new MoveResult(false, reason, new List<IBoardEffect>(), false);
        }
        public override string ToString()
        {
            return $"MoveResult: Succeeded = {Succeeded}, FailureReason = {FailureReason}, Effects = {Effects.Count}, IsComplete = {IsComplete}";
        }
    }
}
