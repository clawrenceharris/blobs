public class RockBlobBehavior : BlobMergeBehavior
{

    public RockBlobBehavior(Blob blob) : base(blob)
    {
    }

    public override void ModifyMergeFromTarget(MergePlan plan, BoardLogic boardLogic)
    {
        plan.EndPosition = _blob.GridPosition - plan.Direction;
        
    }  
   
}