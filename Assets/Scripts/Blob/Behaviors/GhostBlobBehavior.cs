using System.Linq;


/// <summary>
/// The Concrete Strategy for Flag Blobs.
/// This class encapsulates the logic for creating a trail.
/// </summary>
public class GhostBlobBehavior : BlobMergeBehavior
{
    public GhostBlobBehavior(Blob blob) : base(blob)
    {
    }

    public override void ModifyMergeFromTarget(MergePlan plan, BoardLogic board)
    {


        plan.BlobsToRemoveAfterMerge.Add(plan.SourceBlob);

        MergePlan deferredPlan = board.CalculateMergePlan(_blob, plan.SourceBlob);
        plan.DeferredPlan = deferredPlan;

        Blob createdBlob = plan.BlobsToCreateDuringMerge.FirstOrDefault((b) => b.GridPosition.Equals(plan.SourceBlob.GridPosition));
        if(createdBlob != null)
            plan.DeferredPlan.BlobsToRemoveAfterMerge.Add(createdBlob);

    }   
}
