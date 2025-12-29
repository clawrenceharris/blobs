using System;
using System.Collections.Generic;

/// <summary>
/// Manages a history of commands to enable undo functionality.
/// </summary>
public static class MergeInvoker
{
    private static readonly Stack<MergeAction> _merges = new();

    public static event Action<MergeAction> OnMergeExecuted;
    public static event Action<MergeAction> OnMergeUndone;

    public static void ExecuteMerge(MergePlan plan, BoardModel board)
    {
        var merge = new MergeAction(plan);
        merge.Execute(board);
        OnMergeExecuted?.Invoke(merge);
        _merges.Push(merge);
    }
    public static MergeAction UndoMerge(BoardModel board)
    {
        if (_merges.Count > 0)
        {
            var merge = _merges.Pop();
            
            merge.Undo(board);
            OnMergeUndone?.Invoke(merge);
            return merge;
        }
        return null;
    }
    

   
   
    
    
}
