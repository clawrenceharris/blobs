
public class SwitchBlobBehavior : BlobMergeBehavior
{
    
    public SwitchBlobBehavior(Blob blob) : base(blob)
    {
    }
   
    public override void ModifyMergeFromTarget(MergeContext context)
    {
        var plan = context.Plan;
        var board = context.Board;
        plan.BlobsToRemoveOnPath.TryAdd(plan.SourceBlob, _blob.GridPosition);
        // Turn off lasers of matching color
        foreach (var tile in board.Model.TileGrid)
        {
            if (tile is LaserTile laser && laser.LaserColor == plan.TargetBlob.Color)
                plan.OnMergeComplete += (_) =>
                {
                    laser.Toggle();
                };
        }

    }
}