using Blobs.Core;

namespace Blobs.Application
{
    /// <summary>
    /// Immutable read model of the current session for Presentation and UI.
    /// Snapshots are safe to rebuild views from because they do not expose mutable board collections.
    /// </summary>
    public sealed class GameSessionSnapshot
    {
        public GameSessionSnapshot()
        {
            LevelId = string.Empty;
            Board = null;
            MoveCount = 0;
            IsComplete = false;
            CanUndo = false;
            ParMoves = 0;
        }
        /// <summary>
        /// Creates a session snapshot from already-copied board state.
        /// </summary>
        public GameSessionSnapshot(
            string levelId,
            BoardState board,
            int moveCount,
            bool isComplete,
            bool canUndo = false,
            int parMoves = 0)
        {
            LevelId = levelId;
            Board = board;
            MoveCount = moveCount;
            IsComplete = isComplete;
            CanUndo = canUndo;
            ParMoves = parMoves;
        }
        public BoardState Board { get; }
        public string LevelId { get; }
        public int MoveCount { get; }
        public bool IsComplete { get; }
        public bool CanUndo { get; }

        /// <summary>
        /// Soft efficiency target for HUD display. Zero means unset.
        /// </summary>
        public int ParMoves { get; }
    }
}
