using System;
using System.Collections.Generic;

namespace Blobs.Core
{
    public static class LevelFactory
    {
        public static BoardState CreateInitialBoard(LevelDefinition level)
        {
            LevelValidator.ValidateOrThrow(level);


            var blobs = new List<BlobState>(level.Blobs.Count);

            foreach (BlobDefinition blob in level.Blobs)
            {
                blobs.Add(new BlobState(
                    blob.Id,
                    blob.Type,
                    blob.Color,
                    blob.Size,
                    blob.Position
                   ));
            }

            var tiles = new List<TileState>(level.Tiles.Count);
            foreach (TileDefinition tile in level.Tiles)
            {
                tiles.Add(new TileState(
                    tile.Id,
                    tile.Position,
                    tile.Type
                ));
            }

            return new BoardState(
                width: level.Width,
                height: level.Height,
                blobs,
                tiles);
        }

    }
}
