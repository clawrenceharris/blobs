using System.Linq;


/// <summary>
/// The concrete behavior strategy for Ghost Blobs.
/// Ghost blobs modify the merge plan by removing any blob's in the source blob's
/// position after the merge and overtaking it's spot 
/// </summary>
public class GhostBlobBehavior : BlobMergeBehavior
{
    public GhostBlobBehavior(Blob blob) : base(blob)
    {
    }

    public override void ModifyMergeFromTarget(MergePlan plan, BoardModel board)
    {


        plan.BlobsToRemoveAfterMerge.Add(plan.SourceBlob);

        MergePlan deferredPlan = board.CalculateMergePlan(_blob, plan.SourceBlob);
        plan.DeferredPlan = deferredPlan;
        // Find a blob that may have been spawned in the source blob's position during the merge
        Blob createdBlob = plan.BlobsToCreateDuringMerge.FirstOrDefault((b) => b.GridPosition.Equals(plan.SourceBlob.GridPosition));
        // If there is a blob that was spawned, remove it in the deferred plan 
        if(createdBlob != null)
            plan.DeferredPlan.BlobsToRemoveAfterMerge.Add(createdBlob);

    }   
}
