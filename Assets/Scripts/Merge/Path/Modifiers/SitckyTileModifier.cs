using UnityEngine;

namespace Blobs.Core.Merge
{
    /// <summary>
    /// Path modifier: sticky tile forces stop. Future: ice (slide), portal (redirect).
    /// </summary>
    public sealed class StickyTilePathModifier : IPathModifier
    {
        public int Priority => 100;

        public void Apply(IBoardPresenter board, Blob source, ResolvedPath path, PathCell cell,
            ref Vector2Int direction, ref Vector2Int nextPos, out bool stopNow)
        {
            stopNow = false;
            var tile = board.GetTileAt(cell.Pos);
            if (tile != null && tile.Model.Type.IsStickyTile())
            {
                cell.Flags |= PathFlags.StickyStop;
                path.End = cell.Pos;
                path.Termination = PathTermination.ForcedStop;
                stopNow = true;
            }
        }
    }
}
