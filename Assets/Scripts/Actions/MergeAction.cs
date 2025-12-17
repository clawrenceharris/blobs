using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework.Internal;
using UnityEngine;


/// <summary>
/// A composite action that represents a merge. 
/// It bundles multiple subactions occuring during the merge to be executed as a single action.
/// </summary>
public class MergeAction
{
    private readonly List<IAction> _actions;
    public MergePlan Plan { get; private set; }
    public IEnumerator Animate(BoardPresenter presenter) => presenter.AnimateMergeFromPlan(Plan);

    public MergeAction(MergePlan plan)
    {
        Plan = plan;
        _actions = CreateMergeActionsFromPlan(plan);
    }
    private List<IAction> CreateMergeActionsFromPlan(MergePlan plan)
    {
        var actions = new List<IAction>();
        if (plan == null) return new();
        // Create remove actions for blobs to remove
        foreach (var blob in plan.BlobsToRemoveOnPath)
        {
            actions.Add(new RemoveAction(blob.Key));
        }
       
        
        actions.Add(new MoveAction(plan.SourceBlob, plan.StartPosition, plan.EndPosition));
        foreach (var blob in plan.SizeChanges.Keys)
        {
            actions.Add(new ResizeAction(blob, plan.SizeChanges[blob] - blob.Size));
        }
        //Create spawn actions for blobs to spawn; must be after move action
        foreach (var blob in plan.BlobsToCreateOnPath)
        {
            actions.Add(new SpawnAction(blob.Key));
        }
        

        //repeat for deferred merge plan
        actions.AddRange(CreateMergeActionsFromPlan(plan.DeferredPlan));
            
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
