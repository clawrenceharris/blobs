using System.Collections.Generic;

namespace Blobs.Core
{
    public sealed class MoveResult
    {
        public MoveResult(bool succeeded, MoveFailureReason failureReason, IReadOnlyList<IBoardEffect> effects, bool isComplete)
        {
            Succeeded = succeeded;
            FailureReason = failureReason;
            Effects = effects ?? new List<IBoardEffect>();
            IsComplete = isComplete;
        }

        public bool Succeeded { get; }
        public MoveFailureReason FailureReason { get; }
        public IReadOnlyList<IBoardEffect> Effects { get; }
        public bool IsComplete { get; }

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
