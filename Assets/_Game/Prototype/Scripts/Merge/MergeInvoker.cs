using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages command history for moves and undo. Uses MoveResolver + MoveCommand (effects-driven);
/// accepts ICommand so both MoveCommand and legacy MergeAction can be pushed.
/// </summary>
public class MergeInvoker : MonoBehaviour
{
    private static readonly Stack<ICommand> _history = new();

    public static event Action<ICommand> OnMergeExecuted;
    public static event Action<ICommand> OnMergeUndone;
    public static bool CanUndo => _history.Count > 0;
    public static int HistoryCount => _history.Count;

    /// <summary>
    /// Execute a command and add it to history.
    /// </summary>
    public static void Execute(ICommand command)
    {
        if (command == null)
        {
            Debug.LogWarning("[MergeInvoker] Null command");
            return;
        }
        command.Execute();
        _history.Push(command);
        OnMergeExecuted?.Invoke(command);
    }

    /// <summary>
    /// Execute a merge action (legacy). Prefer Execute(ICommand) with MoveCommand.
    /// </summary>
    public static void ExecuteMerge(ICommand command)
    {
        Execute(command);
    }

    /// <summary>
    /// Undo the last move.
    /// </summary>
    public static void UndoMerge()
    {
        if (_history.Count == 0)
        {
            Debug.Log("[MergeInvoker] Nothing to undo");
            return;
        }
        var command = _history.Pop();
        command.Undo();
        OnMergeUndone?.Invoke(command);
    }

    /// <summary>
    /// Clear all command history.
    /// </summary>
    public static void ClearHistory()
    {
        _history.Clear();
    }
}
