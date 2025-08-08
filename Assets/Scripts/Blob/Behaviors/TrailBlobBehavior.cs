
/// <summary>
/// The Concrete Strategy for Trail Blobs.
/// This class encapsulates the logic for creating a trail.
/// </summary>
public class TrailBlobBehavior : BlobMergeBehavior
{
    private new readonly TrailBlob _blob;
    public TrailBlobBehavior(TrailBlob blob) : base(blob)
    {
        _blob = blob;
    }


    public override void ModifyMergeFromSource(MergePlan plan, BoardLogic board)
    {


        // Add a new normal blob at each step of the trail
        var trailElement = new NormalBlob(
            _blob.TrailColor, // Use the trail color
            _blob.Size,
            plan.EndPosition - plan.Direction
        );
        
        Blob currentBlob = board.GetBlobAt(plan.EndPosition - plan.Direction);
        if (currentBlob == null || currentBlob.ID == plan.SourceBlob.ID)
            plan.BlobsToCreateDuringMerge.Add(trailElement);


        base.ModifyMergeFromSource(plan, board);

    }
}
