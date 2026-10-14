using System;

namespace Blobs.Core
{
    /// <summary>
    /// Describes either an explicit source-to-target merge or a directional slide.
    /// </summary>
    public readonly struct MoveIntent
    {
        /// <summary>
        /// Creates a source-to-target merge intent.
        /// </summary>
        public MoveIntent(BlobState source, BlobState target)
        {
            Source = source;
            Target = target;
            Direction = default;
            IsDirectional = false;
        }

        public MoveIntent(BlobState source, Direction direction)
        {
            Source = source;
            Target = null;
            Direction = direction;
            IsDirectional = true;
        }

        /// <summary>
        /// The blob that should move.
        /// </summary>
        public BlobState Source { get; }

        /// <summary>
        /// The blob that the source should move toward, or null for a directional slide.
        /// </summary>
        public BlobState Target { get; }

        /// <summary>
        /// Cardinal direction used by a directional slide.
        /// </summary>
        public Direction Direction { get; }

        /// <summary>
        /// True when resolution should continue toward the board edge without an explicit target.
        /// </summary>
        public bool IsDirectional { get; }


    }
}
