using System;
using System.Collections.Generic;
using Blobs.Core.Merge;
using UnityEngine;

/// <summary>
/// Manages a history of commands to enable undo functionality.
/// </summary>
public class MergeInvoker : MonoBehaviour
{
    private static readonly Stack<MergeAction> _mergeHistory = new();

    public static event Action<MergeAction> OnMergeExecuted;
    public static event Action<MergeAction> OnMergeUndone;
    public static bool CanUndo => _mergeHistory.Count > 0;

    public static int HistoryCount => _mergeHistory.Count;


    /// <summary>
    /// Execute a merge action and add it to history.
    /// </summary>
    public static void ExecuteMerge(MergeAction action)
    {
        if (action == null)
        {
            Debug.LogWarning("[CommandManager] Null command");
            return;
        }
        action.Execute();
        _mergeHistory.Push(action);

        OnMergeExecuted?.Invoke(action);
    }

    /// <summary>
    /// Undo the last merge.
    /// </summary>
    public static void UndoMerge()
    {
        if (_mergeHistory.Count == 0)
        {
            Debug.Log("[CommandManager] Nothing to undo");
            return;
        }

        MergeAction action = _mergeHistory.Pop();
        action.Undo();

        OnMergeUndone?.Invoke(action);
    }

    /// <summary>
    /// Clear all command history.
    /// </summary>
    public void ClearHistory()
    {
        _mergeHistory.Clear();
    }
}
