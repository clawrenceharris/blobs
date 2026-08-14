using System.Collections.Generic;
using Blobs.Core;

namespace Blobs.Application
{
    /// <summary>
    /// Immutable read model of the current session for Presentation and UI.
    /// Snapshots are safe to rebuild views from because they do not expose mutable board collections.
    /// </summary>
    public sealed class GameSessionSnapshot
    {
        /// <summary>
        /// Creates a session snapshot from already-copied board state.
        /// </summary>
        public GameSessionSnapshot(string levelId, IReadOnlyList<BlobState> blobs, IReadOnlyList<TileState> tiles, int moveCount, bool isComplete)
        {
            LevelId = levelId;
            Blobs = blobs;
            Tiles = tiles;
            MoveCount = moveCount;
            IsComplete = isComplete;
        }

        public string LevelId { get; }
        public IReadOnlyList<BlobState> Blobs { get; }
        public IReadOnlyList<TileState> Tiles { get; }
        public int MoveCount { get; }
        public bool IsComplete { get; }
    }
}
