namespace Blobs.Core
{
    public enum MoveFailureReason
    {
        None,
        SourceMissing,
        TargetMissing,
        SameBlob,

        SourceCannotMove,
        NotAligned,
        BlockedPath,
        UnsupportedInteraction,

        NormalMergeRequiresDifferentColors,
        FlagRequiresMatchingColor,
        FlagRequiresNoOtherBlobs
    }
}