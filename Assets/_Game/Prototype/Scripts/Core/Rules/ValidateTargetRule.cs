using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public sealed class ValidateTargetRule : IMoveRule
{
    public int Priority => 100;
    public bool Apply(MoveContext ctx, Queue<IEffect> queue, out MergeFailReason reason)
    {
        reason = MergeFailReason.None;
        if (ctx.Target is not TargetBlob flag)
            return true;

        if (flag.Color != ctx.Source.Color)
        {
            reason = MergeFailReason.TargetColorRuleRejected;
            return false;
        }
        if (ctx.Board.GetBlobsOnBoard().Count(b => b.Model.Type.IsClearable()) > 1)
        {
            reason = MergeFailReason.TargetMergeRuleRejected;
            return false;
        }

        return true;
    }
}
