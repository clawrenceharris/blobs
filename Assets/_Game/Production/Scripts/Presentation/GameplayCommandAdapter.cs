using Blobs.Application;
using UnityEngine;

namespace Blobs.Presentation
{
    /// <summary>
    /// Small UI/editor command bridge for non-pointer gameplay commands.
    /// Board rendering remains in <see cref="BoardPresenter"/>; this component only forwards commands.
    /// </summary>
    public sealed class GameplayCommandAdapter : MonoBehaviour
    {
        private IGameplayCommands _commands;

        /// <summary>
        /// Connects this adapter to the active gameplay command surface.
        /// </summary>
        public void Initialize(IGameplayCommands commands)
        {
            _commands = commands;
        }

        /// <summary>
        /// Restarts the active session. Intended for UI buttons or temporary editor wiring.
        /// </summary>
        public void Restart()
        {
            _commands?.Restart();
        }

        /// <summary>
        /// Undoes the latest board-changing action. Intended for UI buttons or temporary editor wiring.
        /// </summary>
        public bool Undo()
        {
            return _commands != null && _commands.Undo();
        }
    }
}
