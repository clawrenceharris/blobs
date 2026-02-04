using System;
using System.Collections.Generic;
using UnityEngine;

namespace Blobs.Core.Merge
{
    public enum PathTermination
    {
        HitBlob,
        ForcedStop,     // sticky, etc.
        OffBoard,
        BlockedByTile,
        BlockedByLaser,
        NoTargetFound
    }

    public sealed class ResolvedPath
    {
        public string SourceId;
        public Vector2Int Start;
        public Vector2Int End;
        public Vector2Int InitialDirection;
        public string HitBlobId;                // blob at End if termination is HitBlob
        public PathTermination Termination;

        // Every visited cell (in order)
        public List<PathCell> Cells = new();
    }

    public sealed class PathCell
    {
        public Vector2Int Pos;
        public string TileId;   // optional
        public string BlobId;   // optional
        public PathFlags Flags;
    }

    [Flags]
    public enum PathFlags
    {
        None = 0,
        EnteredPortal = 1 << 0,
        Slid = 1 << 1,
        StickyStop = 1 << 2,
        HitBlob = 1 << 3
    }
}