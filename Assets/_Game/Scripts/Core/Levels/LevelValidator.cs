using System;
using System.Collections.Generic;

namespace Blobs.Core
{
    /// <summary>
    /// Validates level definitions before they become playable board state.
    /// </summary>
    public static class LevelValidator
    {
        /// <summary>
        /// Throws when level data is structurally invalid for Core simulation.
        /// </summary>
        public static void ValidateOrThrow(LevelDefinition level)
        {
            if (level == null)
                throw new ArgumentNullException(nameof(level));

            if (string.IsNullOrWhiteSpace(level.Id))
                throw new InvalidOperationException("Level ID is required.");

            if (level.SchemaVersion != LevelDefinition.CurrentSchemaVersion)
            {
                throw new InvalidOperationException(
                    $"Unsupported schema version {level.SchemaVersion}. " +
                    $"Expected {LevelDefinition.CurrentSchemaVersion}.");
            }

            if (level.Width <= 0 || level.Height <= 0)
                throw new InvalidOperationException("Board dimensions must be positive.");


            var blobIds = new HashSet<string>(StringComparer.Ordinal);
            var tileIds = new HashSet<string>(StringComparer.Ordinal);
            var blobPositions = new HashSet<GridPosition>();
            var tilePositions = new HashSet<GridPosition>();

            foreach (BlobDefinition blob in level.Blobs)
            {
                if (blob == null)
                    throw new InvalidOperationException("Level contains an empty blob definition.");

                RequireId(blob.Id, "Blob", blobIds);
                RequireInBounds(level, blob.Position, "Blob");

                if (!blobPositions.Add(blob.Position))
                    throw new InvalidOperationException($"Multiple blobs occupy {blob.Position}.");
                if (blob is ColorBlobDefinition colorBlob)
                {
                    RequireEnumValue(colorBlob.Color, "blob color");
                }
                RequireEnumValue(blob.Type, "blob type");
            }


            foreach (TileDefinition tile in level.Tiles)
            {
                if (tile == null)
                    throw new InvalidOperationException("Level contains an empty tile definition.");

                RequireId(tile.Id, "Tile", tileIds);
                RequireInBounds(level, tile.Position, "Tile");

                if (!tilePositions.Add(tile.Position))
                    throw new InvalidOperationException($"Multiple tiles occupy {tile.Position}.");

                RequireEnumValue(tile.Type, "tile type");
            }

            if (level.Objective == null)
                throw new InvalidOperationException("Level objective is required.");

            RequireEnumValue(level.Objective.Type, "level objective");
        }

        private static void RequireId(string id, string label, ISet<string> ids)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new InvalidOperationException($"{label} ID is required.");

            if (!ids.Add(id))
                throw new InvalidOperationException($"Duplicate {label.ToLowerInvariant()} ID: {id}.");
        }

        private static void RequireEnumValue<T>(T value, string label) where T : struct
        {
            if (!Enum.IsDefined(typeof(T), value))
                throw new InvalidOperationException($"Unsupported {label}: {value}.");
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
