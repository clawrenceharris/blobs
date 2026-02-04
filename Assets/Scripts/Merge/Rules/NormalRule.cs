using Blobs.Core.Merge;

public class NormalRule : IMergeRule
{
    public int Priority => 10;

    public bool Apply(MergeContext ctx, MergePlan plan, out MergeFailReason failReason)
    {
        failReason = MergeFailReason.None;
        if(ctx.Source is not NormalBlob source)
            return true;
        if(ctx.HitBlob is not NormalBlob target)
            return true;
        if(source.Color == target.Color)
        {
            failReason = MergeFailReason.ColorRuleRejected;
            return false;
        }
        

        return true;
    }
}