using System.Diagnostics;

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
            // Source (first-clicked) always moves & survives.
            // HitBlob (second-clicked / target) always gets removed.
            // When both are small, resize source to big after merge.
            if (ctx.Source.Size == BlobSize.Small && ctx.HitBlob.Size == BlobSize.Small)
            {
                plan.Events.Add(new ResizeBlobEvent { BlobId = ctx.Source.ID, From = ctx.Source.Size, To = BlobSize.Big });
            }
            plan.Events.Add(new MergeBlobsEvent
            {
                BlobToRemoveId = ctx.HitBlob.ID,
                BlobToMoveId = ctx.Source.ID,
                To = ctx.HitBlob.GridPosition,
                From = ctx.Source.GridPosition,
            });


            return true;
        }
    }
}