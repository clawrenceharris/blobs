using UnityEngine;

namespace Blobs.Core.Merge
{
    /// <summary>
    /// Path modifiers: sticky (stop), ice (slide), portal (redirect), etc.
    /// Called after a cell is appended, before default termination.
    /// </summary>
   public interface IPathModifier
{
    int Priority { get; }

    /// <summary>
    /// Called after a cell is appended, before default termination.
    /// You may mutate nextPos/direction or stop traversal.
    /// </summary>
    void Apply(BoardModel board, Blob source, ResolvedPath path, PathCell cell,
        ref Vector2Int direction, ref Vector2Int nextPos, out bool stopNow);
}
}