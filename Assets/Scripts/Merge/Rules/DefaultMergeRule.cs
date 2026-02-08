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
            var blobToRemove = ctx.HitBlob;
             // If source blob is smaller than target blob, remove the source blob
            if (ctx.Source.Size < ctx.HitBlob.Size)
            {
                UnityEngine.Debug.Log("Source is smaller");
                blobToRemove = ctx.Source;
            }
            // Resize source blob to big if both are small and remove target blob
            if (ctx.Source.Size == BlobSize.Small && ctx.HitBlob.Size == BlobSize.Small)
            {

                plan.Events.Add(new ResizeBlobEvent { BlobId = ctx.Source.ID, From = ctx.Source.Size, To = BlobSize.Big });
            }
            plan.Events.Add(new MergeBlobsEvent
            {
                BlobToRemoveId = blobToRemove.ID,
                BlobToMoveId = ctx.Source.ID,
                To = ctx.HitBlob.GridPosition,
                From = ctx.Source.GridPosition,
            });


            return true;
        }
    }
}