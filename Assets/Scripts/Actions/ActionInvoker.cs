using System.Collections.Generic;

/// <summary>
/// Manages a history of commands to enable undo functionality.
/// </summary>
public static class ActionInvoker
{
    public static readonly Stack<IAction> _actions = new();

    
    public static void ExecuteAction(IAction action, BoardModel board)
    {
        action.Execute(board);
        _actions.Push(action);
    }

    public static void UndoAction(BoardModel board)
    {
        if (_actions.Count > 0)
        {
            IAction action = _actions.Pop();
            action.Undo(board);
        }
    }
}
