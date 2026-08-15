namespace Blobs.Core
{
    public interface IMergeStrategy
    {
        MergePlan BuildPlan(MoveContext context);
    }
    public sealed class NormalToNormalMergeStrategy : IMergeStrategy
    {
        public MergePlan BuildPlan(MoveContext context)
        {
            if (context.Source.Color == context.Target.Color)
            {
                return MergePlan.Failed(
                    MoveFailureReason.NormalMergeRequiresDifferentColors);
            }

            return MergePlan.Success(
                new RemoveBlobEffect(context.Target),
                new MoveBlobEffect(
                    context.Source.Id,
                    context.Source.Position,
                    context.Target.Position));
        }
    }

    public sealed class NormalToFlagMergeStrategy : IMergeStrategy
    {
        public MergePlan BuildPlan(MoveContext context)
        {
            if (context.Source.Color != context.Target.Color)
            {
                return MergePlan.Failed(
                    MoveFailureReason.FlagRequiresMatchingColor);
            }

            // The board may contain exactly the source and flag.
            if (context.Board.BlobCount != 2)
            {
                return MergePlan.Failed(
                    MoveFailureReason.FlagRequiresNoOtherBlobs);
            }

            return MergePlan.Success(
                new MergeIntoFlagEffect(
                    context.Source,
                    context.Target));
        }
    }

}