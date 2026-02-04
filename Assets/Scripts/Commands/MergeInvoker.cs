using System;
using System.Collections.Generic;
using Blobs.Commands;
using Blobs.Core.Merge;
using UnityEngine;

/// <summary>
/// Manages a history of commands to enable undo functionality.
/// </summary>
public class MergeInvoker : MonoBehaviour
{
    private static readonly Stack<MergeAction> _merges = new();

    public static event Action<MergeAction> OnMergeExecuted;
    public static event Action<MergeAction> OnMergeUndone;
    private static CommandManager _commandManager;

    private void Awake()
    {
        _commandManager = new CommandManager(FindFirstObjectByType<BoardPresenter>());
    }
    /// <summary>Execute a merge using the new event-based plan (MovePlanCommand).</summary>
    public static void ExecuteMerge(MergePlan plan, BoardModel board)
    {
        var mergeAction = new MergeAction(plan, board);
        _commandManager.ExecuteCommand(mergeAction);
        OnMergeExecuted?.Invoke(mergeAction);
        _merges.Push(mergeAction);
    }

    /// <summary>Undo the last merge. Returns the command (e.g. MovePlanCommand with .Plan for animation).</summary>
    public static MergeAction UndoMerge()
    {
        if (_merges.Count > 0)
        {
            MergeAction mergeAction = _merges.Pop();
            _commandManager.Undo();
            OnMergeUndone?.Invoke(mergeAction);
            return mergeAction;
        }
        return null;
    }
    

   
   
    
    
}
