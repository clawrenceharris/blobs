using Blobs.Application;
using UnityEngine;

namespace Blobs.Presentation
{
    public sealed class GameplayCommandAdapter : MonoBehaviour
    {
        private IGameplayCommands _commands;

        public void Initialize(IGameplayCommands commands)
        {
            _commands = commands;
        }

        public void Undo()
        {
            _commands?.UndoLastMove();
        }

        public void Restart()
        {
            _commands?.Restart();
        }
    }
}
