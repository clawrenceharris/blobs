using System.Collections.Generic;

namespace Blobs.Core
{
    public sealed class MoveResult
    {
        public MoveResult(bool succeeded, MoveFailureReason failureReason, IReadOnlyList<IBoardEffect> effects, IReadOnlyList<IBoardEffect> inverseEffects, bool isComplete)
        {
            Succeeded = succeeded;
            FailureReason = failureReason;
            Effects = effects ?? new List<IBoardEffect>();
            InverseEffects = inverseEffects ?? new List<IBoardEffect>();
            IsComplete = isComplete;
        }

        public bool Succeeded { get; }
        public MoveFailureReason FailureReason { get; }
        public IReadOnlyList<IBoardEffect> Effects { get; }
        public IReadOnlyList<IBoardEffect> InverseEffects { get; }
        public bool IsComplete { get; }

        public static MoveResult Failed(MoveFailureReason reason)
        {
            return new MoveResult(false, reason, new List<IBoardEffect>(), new List<IBoardEffect>(), false);
        }
    }
}
