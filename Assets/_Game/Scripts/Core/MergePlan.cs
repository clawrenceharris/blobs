using System.Collections.Generic;
namespace Blobs.Core
{

    public sealed class MergePlan
    {
        private MergePlan(
            bool succeeded,
            MoveFailureReason failureReason,
            IReadOnlyList<IBoardEffect> effects)
        {
            Succeeded = succeeded;
            FailureReason = failureReason;
            Effects = effects;
        }

        public bool Succeeded { get; }
        public MoveFailureReason FailureReason { get; }
        public IReadOnlyList<IBoardEffect> Effects { get; }

        public static MergePlan Success(params IBoardEffect[] effects)
        {
            return new MergePlan(
                true,
                MoveFailureReason.None,
                effects);
        }

        public static MergePlan Failed(MoveFailureReason reason)
        {
            return new MergePlan(
                false,
                reason,
                System.Array.Empty<IBoardEffect>());
        }
    }

}