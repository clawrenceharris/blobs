
/// <summary>
/// The concrete strategy for Flag Blobs behavior.
/// A Flag Blob modifies the merge plan by removing the source blob 
/// after the merge is complete
/// </summary>
public class FlagBlobBehavior : BlobMergeBehavior
{
    public FlagBlobBehavior(Blob blob) : base(blob)
    {
    }

    public override void ModifyMergeFromTarget(MergeContext context)
    {
        context.Plan.BlobsToRemoveOnPath.TryAdd(context.Plan.SourceBlob, _blob.GridPosition);

    }



}