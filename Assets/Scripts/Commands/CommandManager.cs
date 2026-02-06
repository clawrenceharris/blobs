
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Blobs.Commands
{
    /// <summary>
    /// Manages command history for undo/redo functionality.
    /// </summary>
    public class CommandManager
    {

        private Stack<IAction> commandHistory = new();
        public IBoardPresenter Board;
        
        [Header("Settings")]
        [SerializeField] private static int maxHistorySize = 50;

        public int HistoryCount => commandHistory.Count;
        public bool CanUndo => commandHistory.Count > 0;

        public event Action OnCommandExecuted;
        public event Action OnCommandUndone;

        public CommandManager(IBoardPresenter board)
        {
            Board = board;
        }
        /// <summary>
        /// Execute a command and add it to history.
        /// </summary>
        public void ExecuteCommand(IAction command)
        {
            if (command == null)
            {
                Debug.LogWarning("[CommandManager] Null command");
                return;
            }

            command.Execute(this);
            commandHistory.Push(command);

            // Clear redo stack when new command is executed

            // Limit history size
            if (commandHistory.Count > maxHistorySize)
            {
                // Convert to array, remove oldest, convert back
                var tempList = new List<IAction>(commandHistory);
                tempList.RemoveAt(tempList.Count - 1);
                commandHistory = new Stack<IAction>(tempList);
            }

            OnCommandExecuted?.Invoke();
        }

        /// <summary>
        /// Undo the last command.
        /// </summary>
        public void Undo()
        {
            if (commandHistory.Count == 0)
            {
                Debug.Log("[CommandManager] Nothing to undo");
                return;
            }

            IAction command = commandHistory.Pop();
            command.Undo(this);

            OnCommandUndone?.Invoke();
        }

       

        /// <summary>
        /// Clear all command history.
        /// </summary>
        public void ClearHistory()
        {
            commandHistory.Clear();
        }
    }
}