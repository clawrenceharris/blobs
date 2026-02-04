namespace Blobs.Core.Merge
{
    /// <summary>
    /// No-op rule; DefaultMergeRule already removes the hit blob.
    /// Kept for symmetry or future conditional remove logic.
    /// </summary>
    public sealed class RemoveRule : IMergeRule
    {
        public int Priority => 10;

        public bool Apply(MergeContext ctx, MergePlan plan, out MergeFailReason failReason)
        {
            failReason = MergeFailReason.None;
            return true;
        }
    }
}
