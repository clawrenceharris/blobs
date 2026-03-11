using System.Collections.Generic;
using UnityEngine;

public sealed class ValidateLaserRule : IMoveRule
{
    public int Priority => 100;
        public bool Apply(MoveContext ctx, Queue<IEffect> queue, out MergeFailReason reason)
    {
        reason = MergeFailReason.None;
        if (ctx?.Board == null || ctx.Source == null) return true;
        foreach (var cell in ctx.Path)
        {
            if (ctx.Board.IsLaserBlocking(ctx.Source.ID, cell.Pos))
            {
                reason = MergeFailReason.LaserBlocked;
                return false;
            }
        }
        return true;
    }
}
