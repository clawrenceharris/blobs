using System.Collections.Generic;
using Blobs.Core;

namespace Blobs.Application
{
    public sealed class GameSessionSnapshot
    {
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
