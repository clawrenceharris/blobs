using System.Diagnostics;

namespace Blobs.Core.Merge
{
    /// <summary>
    /// No-op rule; DefaultMergeRule already removes the hit blob.
    /// Kept for symmetry or future conditional remove logic.
    /// </summary>
    public sealed class RemoveRule : IMergeRule
    {
        public int Priority => 10;

        public bool Apply(MergeContext ctx, MergePlan plan, out MergeFailReason failReason)
        {
            failReason = MergeFailReason.None;
            if(ctx.HitBlob == null)
                return true;
            // If source blob is smaller than target blob, remove the source blob
            if (ctx.Source.Size < ctx.HitBlob.Size)
            {
                UnityEngine.Debug.Log("Source is smaller");
                plan.Events.Add(new RemoveBlobEvent { BlobId = ctx.Source.ID });
                return true;
            }
            // Resize source blob to big if both are small and remove target blob
            if (ctx.Source.Size == BlobSize.Small && ctx.HitBlob.Size == BlobSize.Small)
            {

                plan.Events.Add(new ResizeBlobEvent { BlobId = ctx.Source.ID, From = ctx.Source.Size, To = BlobSize.Big });
            }
            // Remove target blob if source blob is the same size and not small
            plan.Events.Add(new RemoveBlobEvent { BlobId = ctx.HitBlob.ID });
            return true;
        }
    }
}
