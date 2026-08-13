using System;

namespace Blobs.Core
{
    public readonly struct MoveIntent
    {
        public MoveIntent(string sourceId, string targetId)
        {
            if (string.IsNullOrWhiteSpace(sourceId))
                throw new ArgumentException("Source id cannot be empty.", nameof(sourceId));
            if (string.IsNullOrWhiteSpace(targetId))
                throw new ArgumentException("Target id cannot be empty.", nameof(targetId));

            SourceId = sourceId;
            TargetId = targetId;
        }

        public string SourceId { get; }
        public string TargetId { get; }
    }
}
