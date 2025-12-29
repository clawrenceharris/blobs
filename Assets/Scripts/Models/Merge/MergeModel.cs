using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;


public class MergeContext
{
    public Blob CurrentBlob { get; set; }

    public  MergePlan Plan { get; set; }
   
    public Vector2Int CurrentPos { get; set; }
    public BoardPresenter Board { get; set; }
    public MergeModel Model { get; set; }
}


public class MergePlan
{
    public Blob SourceBlob { get; set; }
    public Blob TargetBlob { get; set; }
    public Vector2Int StartPosition { get; set; }
    public Vector2Int EndPosition { get; set; }
    public Vector2Int Direction
    {
        get
        {
            var direction = EndPosition - StartPosition;
            return new Vector2Int(
                Mathf.Clamp(direction.x, -1, 1),
                Mathf.Clamp(direction.y, -1, 1)
            );
        }
    }
    // public Dictionary<Blob, Vector2Int> BlobsToRemoveAfterMerge { get; private set; } = new();
    public Dictionary<Blob, BlobSize> SizeChanges { get; set; } = new();
    public List<MergePlan> DeferredPlans { get; set; } = new();
    public bool ShouldTerminate { get; set; } = false;
    // public Dictionary<Blob, Vector2Int> BlobsToCreateAfterMerge { get; set; } = new();
    public Dictionary<Blob, Vector2Int> BlobsToCreateOnPath { get; set; } = new();
    public Dictionary<Blob, Vector2Int> BlobsToRemoveOnPath { get; set; } = new();
    public List<Blob> BlobsToRemoveAfterMerge { get; set; } = new();
    public List<Blob> BlobsToCreateAfterMerge { get; set; } = new();

    public Action<MergePlan> OnMergeComplete;

    public static MergePlan operator -(MergePlan plan) => new()
    {
        StartPosition = plan.EndPosition,
        EndPosition = plan.StartPosition,
        TargetBlob = plan.TargetBlob,
        SourceBlob = plan.SourceBlob,
        OnMergeComplete = plan.OnMergeComplete,
        BlobsToRemoveOnPath = plan.BlobsToCreateOnPath,
        BlobsToCreateOnPath = plan.BlobsToRemoveOnPath,
        BlobsToCreateAfterMerge = plan.BlobsToRemoveAfterMerge,
        BlobsToRemoveAfterMerge = plan.BlobsToCreateAfterMerge,
    };
}


public class MergeModel
{

    public static event Action<MergePlan> OnMergeComplete;
    private readonly BoardPresenter _board;
    public MergeModel(BoardPresenter board)
    {
        _board = board;
    }
    public MergePlan CalculateMergePlan(Blob source, Blob target)
    {
        if (!_board.Model.CheckMerge(source.ID, target.ID)) return null;
        var context = new MergeContext()
        {
            Model = this,
            Board = _board,
            Plan = new MergePlan
            {
                SourceBlob = source,
                TargetBlob = target,
                StartPosition = source.GridPosition,
                EndPosition = target.GridPosition,
            }
        };

        var plan = context.Plan;
        var direction = plan.Direction;

        Vector2Int current = source.GridPosition + direction;

        while (current != plan.EndPosition + direction)
        {
            Tile currentTile = _board.Model.GetTileAt(current.x, current.y);
            Blob currentBlob = _board.Model.GetBlobAt(current.x, current.y);

            context.CurrentPos = current;
            context.CurrentBlob = currentBlob;

            if (currentTile == null || !currentTile.Type.IsTraversable()) return null;
            else if (currentBlob != null && !_board.Model.CheckMerge(source.ID, currentBlob.ID)) return null;
            else if (currentBlob != null && !currentBlob.CanMergeWith(source, plan, _board.Model)) return null;
            else if (_board.Model.IsLaserBlocking(source, current)) return null;

            currentTile.Behavior.ModifyMerge(context);
            target.Behavior.ModifyMergeFromTarget(context);
            source.Behavior.ModifyMergeFromSource(context);

            var next = current + direction;
            current = next;

            if (plan.ShouldTerminate)
            {
                break;
            }

        }
        source.Behavior.FinalizeMergeFromSource(context);
        target.Behavior.FinalizeMergeFromTarget(context);
        return plan;
    }
    public Blob GetBlobToRemove(MergeContext context)
    {
        Blob blobToRemove = context.Plan.BlobsToRemoveOnPath.FirstOrDefault(b => b.Value == context.CurrentPos).Key;
        return blobToRemove;
    }
    public Blob GetBlobToSpawn(MergeContext context)
    {
        Blob blobToSpawn = context.Plan.BlobsToCreateOnPath.FirstOrDefault(b => b.Value == context.CurrentPos).Key;
        return blobToSpawn;
    }
    public Blob GetBlobToScale(MergeContext context)
    {
        return context.Plan.SourceBlob;
    }
    public Blob GetBlobToMove(MergeContext context)
    {
        return context.Plan.SourceBlob;
    }
    public IEnumerator DoMerge(MergePlan plan)
    {
        Vector2Int endPosition = plan.EndPosition;
        Vector2Int startPosition = plan.StartPosition;

        Vector2Int direction = plan.Direction;
        Vector2Int currentPos = startPosition;
        var context = new MergeContext
        {
            Board = _board,
            Plan = plan,
            Model = this,
            CurrentBlob = plan.SourceBlob
        };
       
        while (currentPos != endPosition + direction)
        {

            context.CurrentPos = currentPos;
            
            AnimationInvoker.EnqueueAnimation(new MoveAnimationCommand(plan.SourceBlob, context));
            AnimationInvoker.EnqueueAnimation(new ScaleAnimationCommand(plan.SourceBlob, context));
            AnimationInvoker.EnqueueAnimation(new RemoveAnimationCommand(GetBlobToRemove(context), context));
            AnimationInvoker.EnqueueAnimation(new SpawnAnimationCommand(GetBlobToSpawn(context), context));
            
            yield return AnimationInvoker.ExecuteAnimations();

            currentPos += direction;
        }
        foreach (Blob blob in plan.BlobsToRemoveAfterMerge)
        {
            AnimationInvoker.EnqueueAnimation(new RemoveAnimationCommand(blob, context));
        }

        foreach (Blob blob in plan.BlobsToCreateAfterMerge)
        {
            AnimationInvoker.EnqueueAnimation(new SpawnAnimationCommand(blob, context));
        }
        yield return AnimationInvoker.ExecuteAnimations();
        OnMergeComplete?.Invoke(plan);
    }

}