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

            foreach (BlobDefinition blob in level.Blobs)
            {
                var state = new BlobState(
                    blob.Id,
                    blob.Type,
                    blob.Position
                   );
                switch (blob)
                {
                    case NormalBlobDefinition colorBlob:
                        state.AddModel(new ColorBlobModel(colorBlob.Color));
                        break;
                    case TrailBlobDefinition trailBlob:
                        state.AddModel(new ColorBlobModel(trailBlob.Color))
                        .AddModel(new TrailBlobModel(trailBlob.TrailColor));
                        break;
                    case FlagBlobDefinition flagBlob:
                        state.AddModel(new ColorBlobModel(flagBlob.Color));
                        break;

                }
                blobs.Add(state);

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
