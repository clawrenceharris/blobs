using System.Collections.Generic;
using System.Linq;
using UnityEngine;


/// <summary>
/// An composite action that represents a merge. 
/// It bundles multiple subactions occuring during the merge to be executed as a single action.
/// </summary>
public class MergeAction : IAction
{
    private readonly List<IAction> _actions;
    public MergePlan Plan { get; private set; }
    public MergeAction(MergePlan plan)
    {
        Plan = plan;
        _actions = CreateMergeActionsFromPlan(plan);
    }
    private List<IAction> CreateMergeActionsFromPlan(MergePlan plan)
    {
        var actions = new List<IAction>();
       
        //Create remove actions for blobs to remove
        foreach (var blob in plan.BlobsToRemoveDuringMerge)
        {
            actions.Add(new RemoveAction(blob));
        }
        foreach (var blob in plan.BlobsToRemoveAfterMerge)
        {
            actions.Add(new RemoveAction(blob));
        }
        
        actions.Add(new MoveAction(plan.SourceBlob, plan.StartPosition, plan.EndPosition));
        foreach (var blob in plan.SizeChanges.Keys)
        {
            actions.Add(new ResizeAction(blob, plan.SizeChanges[blob] - blob.Size));
        }
        //Create spawn actions for blobs to spawn; must be after move action
        foreach (var blob in plan.BlobsToCreateDuringMerge)
        {
            actions.Add(new SpawnAction(blob));
        }
        foreach (var blob in plan.BlobsToCreateAfterMerge)
        {
            actions.Add(new SpawnAction(blob));
        }

        //repeat for deferred (after effect) merge plan
        if (plan?.DeferredPlan != null)
        {
            foreach (var blob in plan.DeferredPlan.BlobsToRemoveDuringMerge)
            {
                actions.Add(new RemoveAction(blob));
            }
            foreach (var blob in plan.DeferredPlan.BlobsToCreateDuringMerge)
            {
                actions.Add(new SpawnAction(blob));
            }
            foreach (var blob in plan.DeferredPlan.BlobsToRemoveAfterMerge)
            {
                actions.Add(new RemoveAction(blob));
            }
            foreach (var blob in plan.DeferredPlan.BlobsToCreateAfterMerge)
            {
                actions.Add(new SpawnAction(blob));
            }
            actions.Add(new MoveAction(plan.DeferredPlan.SourceBlob, plan.DeferredPlan.StartPosition, plan.DeferredPlan.EndPosition));

        }
            
        return actions;
    }
    public void Execute(BoardModel board)
    {
        foreach (var action in _actions)
        {
            action.Execute(board);
        }
    }

    public void Undo(BoardModel board)
    {
       
        // Undo in reverse order of execution.
        for (int i = _actions.Count - 1; i >= 0; i--)
        {
            _actions[i].Undo(board);
        }
    }
}
