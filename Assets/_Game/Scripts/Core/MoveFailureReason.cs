namespace Blobs.Core
{
    public enum MoveFailureReason
    {
        None,
        SourceMissing,
        TargetMissing,
        SameBlob,
        NotAligned,
        ColorMismatch,
        UnsupportedBlobType,
        BlockedPath
    }
}
