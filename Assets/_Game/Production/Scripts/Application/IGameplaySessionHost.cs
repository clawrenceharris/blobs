using System;

namespace Blobs.Application
{
    /// <summary>
    /// Publishes the currently active gameplay session through Application-facing interfaces.
    /// Unity-side presenters can bind to this contract without referencing the concrete composition root.
    /// </summary>
    public interface IGameplaySessionHost
    {
        /// <summary>
        /// Raised whenever a new session is started and ready for input, presentation, and UI binding.
        /// </summary>
        event Action<IGameplayCommands, IGameplayState> SessionStarted;

        /// <summary>
        /// Current command surface, or null before a level has started.
        /// </summary>
        IGameplayCommands CurrentCommands { get; }

        /// <summary>
        /// Current state surface, or null before a level has started.
        /// </summary>
        IGameplayState CurrentState { get; }
    }
}
