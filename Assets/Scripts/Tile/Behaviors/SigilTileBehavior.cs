public class SigilTileBehavior : TileMergeBehavior
{
    public SigilTileBehavior(Tile tile) : base(tile)
    {
    }

    public override void ModifyMerge(MergePlan plan, BoardLogic board)
    {
        if (plan.SourceBlob is GhostBlob ghostBlob)
        {
            plan.BlobsToRemoveAfterMerge.Add(ghostBlob);
            plan.ShouldTerminate = true;
            plan.EndPosition = _tile.GridPosition;
        }
        
    }
}