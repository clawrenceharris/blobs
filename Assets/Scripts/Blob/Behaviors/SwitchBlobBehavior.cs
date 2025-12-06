
public class SwitchBlobBehavior : BlobMergeBehavior
{
    
    public SwitchBlobBehavior(Blob blob) : base(blob)
    {
    }
   
    public override void ModifyMergeFromTarget(MergePlan plan, BoardModel board)
    {
        plan.BlobsToRemoveAfterMerge.Add(plan.SourceBlob);
        // Turn off lasers of matching color
        foreach (var tile in board.TileGrid)
        {
            if (tile is LaserTile laser && laser.LaserColor == plan.TargetBlob.Color)
                plan.OnMergeComplete += (_) =>
                {
                    laser.Toggle();
                };
        }

        RemoveTargetBlob(plan); // Remove the switch blob after use
    }
}