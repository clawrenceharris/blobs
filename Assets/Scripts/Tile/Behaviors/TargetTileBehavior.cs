public class TargetTileBehavior : TileMergeBehavior
{
    public TargetTileBehavior(Tile tile) : base(tile)
    {
    }

    public override void ModifyMerge(MergePlan plan, BoardLogic board)
    {
        if (board.BlobCount == 1)
        {
            // If this is the only blob on the board, we can remove it immediately
            plan.BlobsToRemoveDuringMerge.Add(plan.SourceBlob);
            return;
        }
    }
}