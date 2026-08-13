using System.Collections.Generic;
using Blobs.Core;

namespace Blobs.Application
{
    public sealed class GameSessionSnapshot
    {
        public GameSessionSnapshot(string levelId, IReadOnlyList<BlobState> blobs, IReadOnlyList<TileState> tiles, int moveCount, bool canUndo, bool isComplete)
        {
            LevelId = levelId;
            Blobs = blobs;
            Tiles = tiles;
            MoveCount = moveCount;
            CanUndo = canUndo;
            IsComplete = isComplete;
        }

        public string LevelId { get; }
        public IReadOnlyList<BlobState> Blobs { get; }
        public IReadOnlyList<TileState> Tiles { get; }
        public int MoveCount { get; }
        public bool CanUndo { get; }
        public bool IsComplete { get; }
    }
}
