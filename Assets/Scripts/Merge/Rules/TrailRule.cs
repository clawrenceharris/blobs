namespace Blobs.Core.Merge
{
    /// <summary>
    /// When source is TrailBlob, spawns trail blobs on each path cell (except final).
    /// Deferred effects (bomb, ghost/sigil) go into plan.DeferredEvents.
    /// </summary>
    public sealed class TrailRule : IMergeRule
    {
        public int Priority => 20;

        public bool Apply(MergeContext ctx, MergePlan plan, out MergeFailReason failReason)
        {
            failReason = MergeFailReason.None;
            if (ctx.Source is not TrailBlob trail)
                return true;

            // Path cells exclude start; include cells stepped onto. Exclude final cell (last in Cells).
            var cells = ctx.Path?.Cells;
            if (cells == null || cells.Count == 0)
                return true;

            int lastIndex = cells.Count - 1;
            for (int i = 0; i < lastIndex; i++)
            {
                var cell = cells[i];
                var pos = cell.Pos;
                if (ctx.Board.GetBlobAt(pos) == null)
                {
                    var blob = new NormalBlob(trail.TrailColor, trail.Size, pos);
                    plan.Events.Add(new SpawnBlobEvent { BlobToSpawn = blob });
                }
            }

            return true;
        }
    }
}
