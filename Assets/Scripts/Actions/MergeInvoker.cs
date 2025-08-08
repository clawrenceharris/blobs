using System;
using System.Collections.Generic;
using System.ComponentModel;

public static class MergeInvoker
{
    private static readonly Stack<MergeAction> _merges = new();

    public static void ExecuteMerge(MergeAction action, BoardLogic board)
    {
        ActionInvoker.ExecuteAction(action, board);
    }
    public static void DisableMerges()
    {
        _merges.Clear();
    }
    public static MergeAction UndoMerge(BoardLogic board)
    {
        
        MergeAction action = _merges.Pop();
        ActionInvoker.UndoAction(board);

        return action;
        
    }
    
   
}