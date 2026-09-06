using System;
using System.Collections.Generic;

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
            GridPosition position,
            BlobComponents components = null
          )
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Blob id cannot be empty.", nameof(id));

            Id = id;
            Type = type;
            Position = position;
            Components = components ?? new BlobComponents();
        }

        public BlobComponents Components { get; }


        public string Id { get; }
        public BlobType Type { get; }
        public GridPosition Position { get; }

        /// <summary>
        /// Creates a copy of this blob at a new grid position while preserving id and traits.
        /// </summary>
        public BlobState WithPosition(GridPosition position) => new(Id, Type, position, Components);
        public BlobState WithComponents(BlobComponents components) => new(Id, Type, Position, components);

        public BlobState WithColor(BlobColor color) => WithComponents(Components.WithColor(color));

        public BlobState WithTrail(BlobColor trailColor) => WithComponents(Components.WithTrail(trailColor));
    }



}
