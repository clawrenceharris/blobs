using UnityEngine;

public class SigilTileBehavior : TileMergeBehavior
{
    public SigilTileBehavior(Tile tile) : base(tile)
    {
    }

    public override void ModifyMerge(MergeContext context)
    {
        if (context.Plan.SourceBlob is GhostBlob)
        {

            RemoveSourceBlob(context, _tile.GridPosition);
            context.Plan.ShouldTerminate = true;
            context.Plan.EndPosition = _tile.GridPosition;
        }
        
    }
}