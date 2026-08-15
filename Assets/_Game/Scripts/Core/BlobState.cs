using System;

namespace Blobs.Core
{
    /// <summary>
    /// Immutable logical state for a blob in Core. Presentation should render this data but not mutate it.
    /// </summary>
    public sealed class BlobState
    {
        /// <summary>
        /// Creates a blob state with a stable id and logical grid position.
        /// </summary>
        public BlobState(
            string id,
            BlobType type,
            BlobColor color,
            GridPosition position,
            BlobColor? trailColor = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Blob id cannot be empty.", nameof(id));

            Id = id;
            Type = type;
            Color = color;
            Position = position;
            TrailColor = trailColor;
        }

        public string Id { get; }
        public BlobType Type { get; }
        public BlobColor Color { get; }
        public GridPosition Position { get; }

        /// <summary>
        /// Color of the normal blobs a Trail blob leaves behind. Null for types without a trail.
        /// </summary>
        public BlobColor? TrailColor { get; }

        public bool IsClearable =>
            Type == BlobType.Normal || Type == BlobType.Trail;

        /// <summary>
        /// Creates a copy of this blob at a new grid position while preserving id and traits.
        /// </summary>
        public BlobState WithPosition(GridPosition position)
        {
            return new BlobState(Id, Type, Color, position, TrailColor);
        }
    }
}
