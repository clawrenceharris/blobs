using System;
using System.Collections.Generic;

namespace Blobs.Core
{
    public static class LevelValidator
    {
        public static void ValidateOrThrow(LevelDefinition level)
        {
            if (level == null)
                throw new ArgumentNullException(nameof(level));

            if (string.IsNullOrWhiteSpace(level.Id))
                throw new InvalidOperationException("Level ID is required.");

            if (level.SchemaVersion <= 0)
                throw new InvalidOperationException("Schema version must be positive.");

            if (level.Width <= 0 || level.Height <= 0)
                throw new InvalidOperationException("Board dimensions must be positive.");

           
            var blobIds = new HashSet<string>();
            var tileIds = new HashSet<string>();

            foreach (BlobDefinition blob in level.Blobs)
            {
                RequireInBounds(level, blob.Position, "Blob");

                if (!blobIds.Add(blob.Id))
                    throw new InvalidOperationException($"Duplicate blob ID: {blob.Id}.");
            }


            foreach (TileDefinition tile in level.Tiles)
            {
                if (tile == null)
                    throw new InvalidOperationException("Level contains an empty tile definition.");

                RequireInBounds(level, tile.Position, "Tile");

                if (!tileIds.Add(tile.Id))
                    throw new InvalidOperationException($"Duplicate tile ID: {tile.Id}.");
            }

          
        }

        private static void RequireInBounds(
            LevelDefinition level,
            GridPosition position,
            string label)
        {
            bool inBounds =
                position.X >= 0 && position.X < level.Width &&
                position.Y >= 0 && position.Y < level.Height;

            if (!inBounds)
            {
                throw new InvalidOperationException(
                    $"{label} at ({position.X}, {position.Y}) is out of bounds.");
            }
        }
    }
}
