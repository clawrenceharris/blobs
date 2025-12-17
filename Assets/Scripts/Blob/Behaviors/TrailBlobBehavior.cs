


using UnityEngine;


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


    public override void ModifyMergeFromSource(MergeContext context)
    {
        var plan = context.Plan;
        var board = context.Board;

        BlobData newBlobData = new BlobData()
        {
            Type = LevelLoader.ToJsonBlobType(BlobType.Normal),
            X = context.CurrentPosition.x - plan.Direction.x,
            Y = context.CurrentPosition.y - plan.Direction.y,
        };
        
        newBlobData.SetProperty(BlobData.Property.Size, LevelLoader.ToJsonSize(_blob.Size));
        newBlobData.SetProperty(BlobData.Property.Color, LevelLoader.ToJsonColor(_blob.TrailColor));


        // Add a new normal blob at each step of the trail
        Blob blobTrail = BlobFactory.CreateBlobModel(newBlobData);

        //check if there is a blob at the position that the blob trail should be
        Blob currentBlob = board.GetBlobAt(blobTrail.GridPosition);
        
        if (currentBlob == null || currentBlob.ID == plan.SourceBlob.ID)
        {
            plan.BlobsToCreateOnPath.TryAdd(blobTrail, blobTrail.GridPosition);
        }

        base.ModifyMergeFromSource(context);

    }
}
