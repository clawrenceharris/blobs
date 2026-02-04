namespace Blobs.Core.Merge
{
   public interface IMergeRule
{
    int Priority { get; }
    bool Apply(MergeContext ctx, MergePlan plan, out MergeFailReason failReason);
}
}