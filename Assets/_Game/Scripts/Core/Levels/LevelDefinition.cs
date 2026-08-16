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
    /// Immutable authored tile data after content has been converted into Core types.
    /// </summary>
    public abstract class TileDefinition
    {
        public string Id { get; }
        public GridPosition Position { get; }
        public TileType Type { get; }

        protected TileDefinition(
            string id,
            GridPosition position,
            TileType type)
        {
            Id = id;
            Position = position;
            Type = type;
        }
    }



    /// <summary>
    /// Basic traversable tile definition.
    /// </summary>
    public sealed class NormalTileDefinition : TileDefinition
    {
        public NormalTileDefinition(
            string id,
            GridPosition position)
            : base(id, position, TileType.Normal)
        {
        }
    }



    /// <summary>
    /// Immutable authored blob data after content has been converted into Core types.
    /// </summary>
    public abstract class BlobDefinition
    {
        public string Id { get; }
        public GridPosition Position { get; }
        public BlobType Type { get; }

        public BlobDefinition(
            string id,
            GridPosition position,
            BlobType type)
        {
            Id = id;
            Position = position;
            Type = type;
        }
    }

    public abstract class ColorBlobDefinition : BlobDefinition
    {
        public BlobColor Color { get; }

        public ColorBlobDefinition(string id, GridPosition position, BlobColor color, BlobType type) : base(id, position, type)
        {
            Color = color;
        }



    }
    /// <summary>
    /// Basic blob definition used by the current source-to-target merge rules.
    /// </summary>
    public sealed class NormalBlobDefinition : ColorBlobDefinition
    {
        public NormalBlobDefinition(
            string id,
            GridPosition position,
            BlobColor color
            )
            : base(id, position, color, BlobType.Normal)
        {
        }
    }
    public sealed class FlagBlobDefinition : ColorBlobDefinition
    {
        public FlagBlobDefinition(
            string id,
            GridPosition position,
            BlobColor color)
            : base(
                id,
                position,
                color,
                BlobType.Flag)
        {
        }
    }

    public sealed class RockBlobDefinition : BlobDefinition
    {
        public RockBlobDefinition(
            string id,
            GridPosition position)
            : base(id, position, BlobType.Rock)
        {
        }
    }

    /// <summary>
    /// Trail blob definition. Moves like a normal blob but leaves normal blobs of
    /// <see cref="TrailColor"/> on the empty tiles it departs during a move.
    /// </summary>
    public sealed class TrailBlobDefinition : ColorBlobDefinition
    {
        public TrailBlobDefinition(
            string id,
            GridPosition position,
            BlobColor color,
            BlobColor trailColor)
            : base(
                id,
                position,
                color,
                BlobType.Trail)
        {
            TrailColor = trailColor;
        }

        public BlobColor TrailColor { get; }
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

        public LevelDefinition(
            string id,
            int schemaVersion,
            int width,
            int height,
            IReadOnlyList<BlobDefinition> blobs,
            IReadOnlyList<TileDefinition> tiles,
            LevelObjectiveDefinition objective = null
           )
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            SchemaVersion = schemaVersion;
            Width = width;
            Height = height;
            Blobs = blobs ?? throw new ArgumentNullException(nameof(blobs));
            Tiles = tiles ?? throw new ArgumentNullException(nameof(tiles));
            Objective = objective ?? LevelObjectiveDefinition.ClearAllClearableBlobs;

        }
    }
}
