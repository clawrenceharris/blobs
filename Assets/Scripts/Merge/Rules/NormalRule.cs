using Blobs.Core.Merge;

public class NormalRule : IMergeRule
{
    public int Priority => 10;

    public bool Apply(MergeContext ctx, MergePlan plan, out MergeFailReason failReason)
    {
        failReason = MergeFailReason.None;
        if(ctx.Source is not NormalBlob source)
            return true;
        if(ctx.HitBlob is not NormalBlob target)
            return true;
        if(source.Color == target.Color)
        {
            failReason = MergeFailReason.ColorRuleRejected;
            return false;
        }
        // If source blob is smaller than target blob, remove the source blob
        if (source.Size < target.Size)
        {
            plan.Events.Add(new RemoveBlobEvent { BlobId = source.ID });
        }
        // Resize source blob to big if both are small and remove target blob
        else if (source.Size == BlobSize.Small && target.Size == BlobSize.Small)
        {
            plan.Events.Add(new ResizeBlobEvent { BlobId = source.ID, From = source.Size, To = BlobSize.Big });
            plan.Events.Add(new RemoveBlobEvent { BlobId = target.ID });
        }
        // Remove target blob if source blob is the same size and not small
        else
        {
            plan.Events.Add(new RemoveBlobEvent { BlobId = target.ID });
        }
        

        return true;
    }
}