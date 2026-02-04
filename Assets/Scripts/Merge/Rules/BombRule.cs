using UnityEngine;

namespace Blobs.Core.Merge
{
    /// <summary>
    /// Placeholder for bomb behavior. When source or hit blob is BombBlob,
    /// deferred remove events (adjacent + bomb + partner) go into plan.DeferredEvents.
    /// </summary>
    public sealed class BombRule : IMergeRule
    {
        public int Priority => 50;

        public bool Apply(MergeContext ctx, MergePlan plan, out MergeFailReason failReason)
        {
            failReason = MergeFailReason.None;
            // TODO: if (ctx.Source is BombBlob || ctx.HitBlob is BombBlob) add deferred remove events
            // (adjacent blobs + source + hit) to plan.DeferredEvents.
            return true;
        }
    }
}
