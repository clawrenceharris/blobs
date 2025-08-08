
/// <summary>
/// The Concrete Strategy for Flag Blobs.
/// This class encapsulates the logic for creating a trail.
/// </summary>
public class FlagBlobBehavior : BlobMergeBehavior
{
    public FlagBlobBehavior(Blob blob) : base(blob)
    {
    }

    public override void ModifyMergeFromTarget(MergePlan plan, BoardLogic board)
    {
        plan.BlobsToRemoveAfterMerge.Add(plan.SourceBlob);

    }
    


}