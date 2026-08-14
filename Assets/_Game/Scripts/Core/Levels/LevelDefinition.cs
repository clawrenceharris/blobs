using System;
using System.Collections.Generic;

namespace Blobs.Core
{
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
        public BlobSize Size { get; }
        public BlobColor Color { get; }
        public BlobType Type { get; }

        public BlobDefinition(
            string id,
            GridPosition position,
            BlobColor color,
            BlobSize size,
            BlobType type)
        {
            Id = id;
            Position = position;
            Size = size;
            Color = color;
            Type = type;
        }
    }
    /// <summary>
    /// Basic blob definition used by the current source-to-target merge rules.
    /// </summary>
    public sealed class NormalBlobDefinition : BlobDefinition
    {
        public NormalBlobDefinition(
            string id,
            GridPosition position,
            BlobColor color,
            BlobSize size
            )
            : base(id, position, color, size, BlobType.Normal)
        {
        }
    }

    /// <summary>
    /// Unity-free level data consumed by Core and Application when starting a session.
    /// </summary>
    public sealed class LevelDefinition
    {
        public string Id { get; }
        public int Width { get; }
        public int Height { get; }
        public IReadOnlyList<BlobDefinition> Blobs { get; }
        public IReadOnlyList<TileDefinition> Tiles { get; }
        public int SchemaVersion { get; }

        public LevelDefinition(
            string id,
            int schemaVersion,
            int width,
            int height,
            IReadOnlyList<BlobDefinition> blobs,
            IReadOnlyList<TileDefinition> tiles
           )        
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            SchemaVersion = schemaVersion;
            Width = width;
            Height = height;
            Blobs = blobs ?? throw new ArgumentNullException(nameof(blobs));
            Tiles = tiles ?? throw new ArgumentNullException(nameof(tiles));
            
        }
    }
}
