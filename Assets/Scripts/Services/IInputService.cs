using UnityEngine;
using System;

namespace Blobs.Services
{
    /// <summary>
    /// Input events that can be triggered by user input.
    /// SRP: Only handles input detection and event emission.
    /// </summary>
    public interface IInputService
    {
        /// <summary>Fired when user clicks at a world position</summary>
        event Action<Vector2> OnClickAtPosition;

        /// <summary>Fired when user presses a direction key (WASD/Arrows)</summary>
        event Action<Vector2Int> OnDirectionInput;

        /// <summary>Fired when user presses undo</summary>
        event Action OnUndoPressed;

        /// <summary>Fired when user presses redo</summary>
        event Action OnRedoPressed;

        /// <summary>Whether input is currently blocked</summary>
        bool IsInputBlocked { get; }

        /// <summary>Temporarily block input for a duration</summary>
        void BlockInput(float duration);

        /// <summary>Set input enabled/disabled</summary>
        void SetInputEnabled(bool enabled);
    }
}
