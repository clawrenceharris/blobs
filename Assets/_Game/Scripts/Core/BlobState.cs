using System;

namespace Blobs.Core
{
    public sealed class BlobState
    {
        public BlobState(string id, BlobType type, BlobColor color, BlobSize size, GridPosition position)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Blob id cannot be empty.", nameof(id));

            Id = id;
            Type = type;
            Color = color;
            Size = size;
            Position = position;
        }

        public string Id { get; }
        public BlobType Type { get; }
        public BlobColor Color { get; }
        public BlobSize Size { get; }
        public GridPosition Position { get; }
        public bool IsClearable => Type == BlobType.Normal;

        public BlobState WithPosition(GridPosition position)
        {
            return new BlobState(Id, Type, Color, Size, position);
        }
    }
}
