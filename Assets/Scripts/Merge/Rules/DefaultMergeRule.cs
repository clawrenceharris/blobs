namespace Blobs.Core.Merge
{
    /// <summary>
    /// Default merge rule: move source to end, remove hit blob. Same-color reject.
    /// Deferred effects (bomb, ghost/sigil) go into plan.DeferredEvents via other rules.
    /// </summary>
    public sealed class DefaultMergeRule : IMergeRule
    {
        public int Priority => 10;

        public bool Apply(MergeContext ctx, MergePlan plan, out MergeFailReason fail)
        {
            fail = MergeFailReason.None;

            if (ctx.Path.Termination == PathTermination.HitBlob && ctx.HitBlob == null)
            {
                fail = MergeFailReason.NoTargetInDirection;
                return false;
            }

            // blobs must be on the same row or column
            if (ctx.HitBlob != null)
            {
                if (ctx.Source.GridPosition.x != ctx.HitBlob.GridPosition.x && ctx.Source.GridPosition.y != ctx.HitBlob.GridPosition.y)
                {
                    fail = MergeFailReason.NotAligned;
                    return false;
                }
               
            }
            // Move happens (source slides to end)
            plan.Events.Add(new MoveBlobEvent
            {
                BlobId = ctx.Source.ID,
                From = ctx.Path.Start,
                To = ctx.Path.End
            });


            return true;
        }
    }
}