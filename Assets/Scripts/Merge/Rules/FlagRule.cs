using System.Linq;
using Blobs.Core.Merge;
using UnityEngine;

/// <summary>
/// Merge rule for flag blobs. If the hit blob is a flag blob and the source blob is not the same color, 
///  and there are no other (clearable) blobs on the board then the source blob is removed
/// </summary>
public class FlagRule : IMergeRule
{
    public int Priority => 10;

    public bool Apply(MergeContext ctx, MergePlan plan, out MergeFailReason failReason)
    {
        failReason = MergeFailReason.None;

        if (ctx.HitBlob is not FlagBlob flag)
            return true;

        if (flag.Color != ctx.Source.Color)
        {
            failReason = MergeFailReason.FlagColorRuleRejected;
            return false;
        }
        if (ctx.Board.GetAllBlobs().OfType<IClearable>().Count() > 1)
        {
            failReason = MergeFailReason.FlagMergeRuleRejected;
            return false;
        }

        plan.Events.Add(new RemoveBlobEvent { BlobId = ctx.Source.ID });

        return true;
    }
}