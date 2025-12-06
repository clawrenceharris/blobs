
/// <summary>
/// The concrete strategy for Trail Blobs.
/// This class encapsulates the logic for creating a trail.
/// </summary>
public class TrailBlobBehavior : BlobMergeBehavior
{
    private new readonly TrailBlob _blob;
    public TrailBlobBehavior(TrailBlob blob) : base(blob)
    {
        _blob = blob;
    }


    public override void ModifyMergeFromSource(MergePlan plan, BoardModel board)
    {

        BlobData newBlobData = new BlobData()
        {
            Type = LevelLoader.ToJsonBlobType(BlobType.Normal),
            X = plan.EndPosition.x - plan.Direction.x,
            Y = plan.EndPosition.y - plan.Direction.y,


        };
        newBlobData.SetProperty(BlobData.Property.Size, LevelLoader.ToJsonSize(_blob.Size));
        newBlobData.SetProperty(BlobData.Property.Color, LevelLoader.ToJsonColor(_blob.TrailColor));

        

        // Add a new normal blob at each step of the trail

        Blob blobTrail = BlobFactory.CreateBlobModel(newBlobData);
    
        
        
        Blob currentBlob = board.GetBlobAt(plan.EndPosition - plan.Direction);
        if (currentBlob == null || currentBlob.ID == plan.SourceBlob.ID)
            plan.BlobsToCreateDuringMerge.Add(blobTrail);


        base.ModifyMergeFromSource(plan, board);

    }
}
