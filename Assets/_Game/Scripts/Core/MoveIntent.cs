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
        public MoveIntent(string sourceId, string targetId)
        {
            if (string.IsNullOrWhiteSpace(sourceId))
                throw new ArgumentException("Source id cannot be empty.", nameof(sourceId));
            if (string.IsNullOrWhiteSpace(targetId))
                throw new ArgumentException("Target id cannot be empty.", nameof(targetId));

            SourceId = sourceId;
            TargetId = targetId;
        }

        /// <summary>
        /// Id of the blob that should move.
        /// </summary>
        public string SourceId { get; }

        /// <summary>
        /// Id of the blob that the source is moving toward.
        /// </summary>
        public string TargetId { get; }
    }
}
