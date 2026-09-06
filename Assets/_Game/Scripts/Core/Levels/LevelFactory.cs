using System;
using System.Collections.Generic;

namespace Blobs.Core
{
    /// <summary>
    /// Builds mutable board state from immutable level definitions.
    /// </summary>
    public static class LevelFactory
    {
        /// <summary>
        /// Validates level data and creates the authored initial board state used for start and restart.
        /// </summary>
        public static BoardState CreateInitialBoard(LevelDefinition level)
        {
            LevelValidator.ValidateOrThrow(level);


            var blobs = new List<BlobState>(level.Blobs.Count);

            foreach (BlobDefinition b in level.Blobs)
            {
                var blob = new BlobState(
                    b.Id,
                    b.Type,
                    b.Position);
                switch (b)
                {
                    case NormalBlobDefinition colorBlob:
                        blob = blob.WithColor(colorBlob.Color);
                        break;
                    case TrailBlobDefinition trailBlob:
                        blob = blob.WithTrail(trailBlob.TrailColor).WithColor(trailBlob.Color);
                        break;
                    case FlagBlobDefinition flagBlob:
                        blob = blob.WithColor(flagBlob.Color);
                        break;


                }
                blobs.Add(blob);

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
                tiles,
                emptyPositions: level.EmptyPositions
                );
        }

    }
}
