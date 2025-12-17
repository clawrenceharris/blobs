public class TargetTileBehavior : TileMergeBehavior
{
    public TargetTileBehavior(Tile tile) : base(tile)
    {
    }

    public override void ModifyMerge(MergeContext context)
    {
        if (context.Board.BlobCount == 1)
        {
            // If this is the only blob on the board, we can remove it immediately
            RemoveSourceBlob(context, _tile.GridPosition);
            return;
        }
    }
}