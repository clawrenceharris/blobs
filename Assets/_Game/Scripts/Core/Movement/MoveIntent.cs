using System;

namespace Blobs.Core
{
    /// <summary>
    /// Describes the player's intended merge using explicit source and target blob ids.
    /// Click order matters: the source is the first selected blob and the target is the second.
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

        }

        /// <summary>
        /// The blob that should move.
        /// </summary>
        public BlobState Source { get; }

        /// <summary>
        /// The blob that the source should move toward.
        /// </summary>
        public BlobState Target { get; }


    }
}
