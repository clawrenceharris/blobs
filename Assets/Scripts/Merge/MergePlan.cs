using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Blobs.Core.Merge
{
    /// <summary>
    /// Context for batch rules: board snapshot, source blob, hit blob (if any), resolved path.
    /// Rules: merge (default), trail, bomb (deferred remove), ghost/sigil (deferred remove/condition), laser-switch (future).
    /// </summary>
    public sealed class MergeContext
    {
        public IBoardPresenter Board;
        public Blob Source;
        public Blob HitBlob; // may be null
        public ResolvedPath Path;
    }
    /// <summary>
    /// Event-based move plan. Events run in order; DeferredEvents (e.g. bomb, ghost/sigil) run after.
    /// </summary>
    public sealed class MergePlan
    {
        public string SourceId;
        public string HitBlobId;          // can be null (forced stop or special move)
        public ResolvedPath Path;

        // Ordered micro-actions to execute/animate.
        public List<IMergeEvent> Events = new();

        // If you want explicit phases:
        public List<IMergeEvent> DeferredEvents = new(); // optional
    }
    /// <summary>
    /// Describes the actions to perform at a single position along the merge path.
    /// </summary>
    public sealed class MergeStep
    {
        public Vector2Int Pos;
        public Tile Tile;          // nullable
        public Blob Blob;          // nullable
        public StepEventFlags Flags; // enteredPortal, stickyStop, etc.
    }


    [Flags]
    public enum StepEventFlags
    {
        None = 0,
        EnteredPortal = 1 << 0,
        SlidOnIce = 1 << 1,
        StickyStopped = 1 << 2,
        LaserBlocked = 1 << 3,
        HitTargetBlob = 1 << 4
    }


}