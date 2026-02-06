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
            // // Move happens (source slides to end)
            plan.Events.Add(new MergeBlobsEvent
            {
                BlobId = ctx.Source.ID,
                HitBlobId = ctx.HitBlob.ID,
                To = ctx.HitBlob.GridPosition,
                From = ctx.Source.GridPosition,
            });


            return true;
        }
    }
}