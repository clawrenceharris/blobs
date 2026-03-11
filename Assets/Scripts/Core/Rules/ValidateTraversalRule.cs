

using System.Collections.Generic;
using UnityEngine;

public sealed class ValidateTraversalRule : IMoveRule
{
    public int Priority => 100;
    public bool Apply(MoveContext ctx, Queue<IEffect> queue, out MergeFailReason reason)
    {
        reason = MergeFailReason.None;
        if (ctx?.Path == null) return true;
        foreach (var cell in ctx.Path)
        {
            if (cell.Tile == null || !cell.Tile.Type.IsTraversable())
            {
                reason = MergeFailReason.TileBlocked;
                return false;
            }
        }
        return true;
    }
}