using System;
using Blobs.Core;

namespace Blobs.Application
{
    /// <summary>
    /// Publishes the currently active gameplay session through Application-facing interfaces.
    /// Unity-side presenters can bind to this contract without referencing the concrete composition root.
    /// </summary>
    public interface IGameplaySessionHost
    {
        /// <summary>
        /// Current level index.
        /// </summary>
        int CurrentLevelIndex { get; }


        /// <summary>
        /// Whether the current level is a tutorial.
        /// </summary>
        bool IsTutorial { get; }


        LevelDefinition CurrentLevel { get; }
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

        /// <summary>
        /// Exits the game.
        /// </summary>
        void ExitGame();

        /// <summary>
        /// Loads the previous level.
        /// </summary>
        void PreviousLevel();

        /// <summary>
        /// Loads the next level.
        /// </summary>
        void NextLevel();
    }

    /// <summary>
    /// Optional timing surface for UI that must wait until board presentation has settled.
    /// Logical session snapshots remain immediate so command availability is never animation-bound.
    /// </summary>
    public interface IGameplayPresentationStatus
    {
        /// <summary>
        /// Raised after the board has finished presenting, or has synchronously rebuilt, a snapshot.
        /// </summary>
        event Action<GameSessionSnapshot> PresentationSettled;

        /// <summary>
        /// Most recent snapshot whose board presentation has settled.
        /// </summary>
        GameSessionSnapshot PresentedSnapshot { get; }
    }
}
