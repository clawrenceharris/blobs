using System.Collections.Generic;
using UnityEngine;

public sealed class ValidateIntentRule : IMoveRule
{
    public int Priority => 100;
    public bool Apply(MoveContext ctx, Queue<IEffect> queue, out MergeFailReason reason)
    {
        reason = MergeFailReason.None;
        if (ctx?.Source == null) { reason = MergeFailReason.InvalidSource; return false; }
        if (ctx.Source.ID == ctx.Target?.ID) { reason = MergeFailReason.InvalidTarget; return false; }
        if (ctx.Target != null)
        {
            var dx = ctx.Target.GridPosition.x - ctx.Source.GridPosition.x;
            var dy = ctx.Target.GridPosition.y - ctx.Source.GridPosition.y;
            if (dx != 0 && dy != 0) { reason = MergeFailReason.NotAligned; return false; }
        }
        return true;
    }
}