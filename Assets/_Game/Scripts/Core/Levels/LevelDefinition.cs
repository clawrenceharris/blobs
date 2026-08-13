using System;
using System.Collections.Generic;

namespace Blobs.Core
{
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

   

    public sealed class NormalTileDefinition : TileDefinition
    {
        public NormalTileDefinition(
            string id,
            GridPosition position)
            : base(id, position, TileType.Normal)
        {
        }
    }

    

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
