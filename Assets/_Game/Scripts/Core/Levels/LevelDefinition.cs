using System;
using System.Collections.Generic;

namespace Blobs.Core
{
    /// <summary>
    /// One counter in a level objective: collect <see cref="AmountNeeded"/> of <see cref="Target"/>.
    /// </summary>
    public sealed class LevelObjectiveItemDefinition
    {
        public LevelObjectiveItemDefinition(ObjectiveTarget target, int amountNeeded)
        {
            Target = target;
            AmountNeeded = amountNeeded;
        }

        public ObjectiveTarget Target { get; }
        public int AmountNeeded { get; }
    }

    /// <summary>
    /// Immutable objective data authored for a level. An objective with no items keeps the
    /// original rule: clear every clearable blob.
    /// </summary>
    public sealed class LevelObjectiveDefinition
    {
        public LevelObjectiveDefinition(IReadOnlyList<LevelObjectiveItemDefinition> items)
        {
            Items = items ?? throw new ArgumentNullException(nameof(items));
        }

        public IReadOnlyList<LevelObjectiveItemDefinition> Items { get; }

        public static LevelObjectiveDefinition ClearAllClearableBlobs { get; } =
            new LevelObjectiveDefinition(Array.Empty<LevelObjectiveItemDefinition>());
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

        public LevelDefinition(
            string id,
            int schemaVersion,
            int width,
            int height,
            IReadOnlyList<BlobDefinition> blobs,
            IReadOnlyList<TileDefinition> tiles,
            IReadOnlyList<GridPosition> emptyPositions = null,
            LevelObjectiveDefinition objective = null)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            SchemaVersion = schemaVersion;
            Width = width;
            Height = height;
            Blobs = blobs ?? throw new ArgumentNullException(nameof(blobs));
            Tiles = tiles ?? throw new ArgumentNullException(nameof(tiles));
            EmptyPositions = emptyPositions ?? new List<GridPosition>();
            Objective = objective ?? LevelObjectiveDefinition.ClearAllClearableBlobs;
        }
    }
}
