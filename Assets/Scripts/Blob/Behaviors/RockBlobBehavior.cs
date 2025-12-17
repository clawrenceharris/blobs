public class RockBlobBehavior : BlobMergeBehavior
{

    public RockBlobBehavior(Blob blob) : base(blob)
    {
    }

    public override void ModifyMergeFromTarget(MergeContext context)
    {
        context.Plan.EndPosition = _blob.GridPosition - context.Plan.Direction;
        
    }  
   
}