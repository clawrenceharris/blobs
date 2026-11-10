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
                blobs.Add(CreateBlob(b));

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

        /// <summary>
        /// Creates the initial state for one authored blob.
        /// </summary>
        public static BlobState CreateBlob(BlobDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            var blob = new BlobState(
                definition.Id,
                definition.Type,
                definition.Position)
                .WithFacing(definition.Facing);
            switch (definition)
            {
                case NormalBlobDefinition normal:
                    blob = blob
                    .WithColor(normal.Color)
                    .WithSize(normal.Size);
                    break;
                case TrailBlobDefinition trail:
                    blob = blob
                    .WithTrail(trail.TrailColor)
                    .WithColor(trail.Color)
                    .WithSize(trail.Size);
                    break;
                case NestBlobDefinition nest:
                    blob = blob.WithColor(nest.Color);
                    break;
            }

            return blob;
        }

    }
}
