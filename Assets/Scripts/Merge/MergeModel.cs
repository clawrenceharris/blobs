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
        public BoardModel Board;
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


    [System.Flags]
    public enum StepEventFlags
    {
        None = 0,
        EnteredPortal = 1 << 0,
        SlidOnIce = 1 << 1,
        StickyStopped = 1 << 2,
        LaserBlocked = 1 << 3,
        HitTargetBlob = 1 << 4
    }

   

    


    
    public class MergeModel
    {
        // public event Action<MergePlan> OnMergeComplete;
        private readonly BoardPresenter _board;
        public MergeModel(BoardPresenter board)
        {
            _board = board;
        }
        // public MergePlan CalculateMergePlan(Blob source, Blob target)
        // {
        //     if (!_board.Model.CheckMerge(source.ID, target.ID)) return null;
        //     var context = new MergeContext()
        //     {
        //         Model = this,
        //         Board = _board,
        //         Plan = new MergePlan
        //         {
        //             SourceBlob = source,
        //             TargetBlob = target,
        //             StartPosition = source.GridPosition,
        //             EndPosition = target.GridPosition,
        //         }
        //     };

        //     var plan = context.Plan;
        //     var direction = plan.Direction;

        //     Vector2Int current = source.GridPosition + direction;

        //     while (current != plan.EndPosition + direction)
        //     {
        //         Tile currentTile = _board.Model.GetTileAt(current.x, current.y);
        //         Blob currentBlob = _board.Model.GetBlobAt(current.x, current.y);

        //         context.CurrentPos = current;
        //         context.CurrentBlob = currentBlob;

        //         if (currentTile == null || !currentTile.Type.IsTraversable()) return null;
        //         else if (currentBlob != null && !_board.Model.CheckMerge(source.ID, currentBlob.ID)) return null;
        //         else if (currentBlob != null && !currentBlob.CanMergeWith(source, plan, _board.Model)) return null;
        //         else if (_board.Model.IsLaserBlocking(source, current)) return null;

        //         currentTile.Behavior.ModifyMerge(context);
        //         target.Behavior.ModifyMergeFromTarget(context);
        //         source.Behavior.ModifyMergeFromSource(context);

        //         var next = current + direction;
        //         current = next;

        //         if (plan.ShouldTerminate)
        //         {
        //             break;
        //         }

        //     }
        //     source.Behavior.FinalizeMergeFromSource(context);
        //     target.Behavior.FinalizeMergeFromTarget(context);
        //     BuildSteps(plan);
        //     return plan;
        // }

        // private void BuildSteps(MergePlan plan)
        // {
        //     plan.Steps.Clear();
        //     Vector2Int currentPos = plan.StartPosition;
        //     Vector2Int direction = plan.Direction;
        //     Vector2Int endPosition = plan.EndPosition;

        //     while (currentPos != endPosition + direction)
        //     {
        //         var step = new MergeStep { Position = currentPos };
        //         step.BlobToRemove = plan.BlobsToRemoveOnPath.FirstOrDefault(b => b.Value == currentPos).Key;
        //         step.BlobToSpawn = plan.BlobsToCreateOnPath.FirstOrDefault(b => b.Value == currentPos).Key;
        //         if (step.BlobToRemove != null && plan.SizeChanges.TryGetValue(plan.SourceBlob, out BlobSize newSize))
        //             step.SourceNewSize = newSize;
        //         plan.Steps.Add(step);
        //         currentPos += direction;
        //     }
        // }

        /// <summary>
        /// Builds a plan for reverse/undo animation. Call after MergeInvoker.UndoMerge has restored model state.
        /// </summary>
        // public MergePlan BuildReversePlan(MergePlan plan)
        // {
        //     var reversedDeferred = plan.DeferredPlans.Select(BuildReversePlan).ToList();
        //     reversedDeferred.Reverse();
        //     var reversed = new MergePlan
        //     {
        //         SourceBlob = plan.TargetBlob,
        //         TargetBlob = plan.SourceBlob,
        //         StartPosition = plan.EndPosition,
        //         EndPosition = plan.StartPosition,
        //         BlobsToRemoveOnPath = plan.BlobsToCreateOnPath,
        //         BlobsToCreateOnPath = plan.BlobsToRemoveOnPath,
        //         BlobsToRemoveAfterMerge = plan.BlobsToCreateAfterMerge,
        //         BlobsToCreateAfterMerge = plan.BlobsToRemoveAfterMerge,
        //         DeferredPlans = reversedDeferred
        //     };
        //     BuildSteps(reversed);
        //     return reversed;
        // }
    }
}