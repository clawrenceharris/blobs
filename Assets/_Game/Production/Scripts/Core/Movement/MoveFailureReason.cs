namespace Blobs.Core
{
    public enum MoveFailureReason
    {
        None,
        SourceOrTargetMissing,
        SameBlob,
        MoveTimeout,
        SourceCannotMove,
        NotAligned,
        PathBlocked,
        UnsupportedInteraction,

        NormalMergeRequiresDifferentColors,
        FlagRequiresMatchingColor,
        FlagRequiresNoOtherBlobs,
        FlagRequiresNormalSource,
        FlagCaptureRequired
    }
}
