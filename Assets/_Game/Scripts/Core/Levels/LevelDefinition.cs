using System;
using System.Collections.Generic;

namespace Blobs.Core
{
    /// <summary>
    /// Objectives supported by the current production content schema.
    /// </summary>
    public enum LevelObjectiveType
    {
        ClearAllClearableBlobs
    }

    /// <summary>
    /// Immutable objective data authored for a level.
    /// </summary>
    public sealed class LevelObjectiveDefinition
    {
        public LevelObjectiveDefinition(LevelObjectiveType type)
        {
            Type = type;
        }

        public LevelObjectiveType Type { get; }

        public static LevelObjectiveDefinition ClearAllClearableBlobs { get; } =
            new LevelObjectiveDefinition(LevelObjectiveType.ClearAllClearableBlobs);
    }


    /// <summary>
    /// Unity-free level data consumed by Core and Application when starting a session.
    /// </summary>
    public sealed class LevelDefinition
    {
        public const int CurrentSchemaVersion = 1;

        public string Id { get; }
        public int Width { get; }
        public int Height { get; }
        public IReadOnlyList<BlobDefinition> Blobs { get; }
        public IReadOnlyList<TileDefinition> Tiles { get; }
        public int SchemaVersion { get; }
        public LevelObjectiveDefinition Objective { get; }
        public IReadOnlyList<GridPosition> EmptyPositions { get; }

        /// <summary>
        /// Soft efficiency target for HUD display (current/par). Zero means unset; not a fail budget.
        /// </summary>
        public int ParMoves { get; }

        public LevelDefinition(
            string id,
            int schemaVersion,
            int width,
            int height,
            IReadOnlyList<BlobDefinition> blobs,
            IReadOnlyList<TileDefinition> tiles,
            IReadOnlyList<GridPosition> emptyPositions = null,
            LevelObjectiveDefinition objective = null,
            int parMoves = 0)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            SchemaVersion = schemaVersion;
            Width = width;
            Height = height;
            Blobs = blobs ?? throw new ArgumentNullException(nameof(blobs));
            Tiles = tiles ?? throw new ArgumentNullException(nameof(tiles));
            EmptyPositions = emptyPositions ?? new List<GridPosition>();
            Objective = objective ?? LevelObjectiveDefinition.ClearAllClearableBlobs;
            if (parMoves < 0)
                throw new ArgumentOutOfRangeException(nameof(parMoves), parMoves, "Par moves cannot be negative.");
            ParMoves = parMoves;
        }
    }
}
