using System;
using System.Collections.Generic;

/// <summary>
/// Manages a history of commands to enable undo functionality.
/// </summary>
public static class ActionInvoker
{
    private static readonly Stack<IAction> _actions = new();

    public static event Action<IAction> OnActionExecuted;
    public static event Action<IAction> OnActionUndone;

    public static void ExecuteAction(IAction action, BoardModel board)
    {
        _actions.Push(action);
        action.Execute(board);
        OnActionExecuted?.Invoke(action);

    }
    public static void UndoAction(BoardModel board)
    {
        if(_actions.Count > 0)
        {
            var action = _actions.Pop();
            action.Undo(board);
            OnActionUndone?.Invoke(action);
        }
    }
    

   
   
    
    
}
