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
        FlagRequiresNoOtherBlobsOfSameColor,
        FlagRequiresNormalSource,
        FlagCaptureRequired,
        NormalMergeRequiresSize,

    }

    public static class MoveFailureReasonExtensions
    {
        /// <summary>
        /// True for rule failures the player should be warned about before committing. Other reasons
        /// are either prevented by input shaping (alignment, reach) or are configuration gaps.
        /// </summary>
        public static bool IsPlayerFacing(this MoveFailureReason reason)
        {
            switch (reason)
            {
                case MoveFailureReason.NormalMergeRequiresDifferentColors:
                case MoveFailureReason.NormalMergeRequiresSize:
                case MoveFailureReason.FlagRequiresMatchingColor:
                case MoveFailureReason.FlagRequiresNoOtherBlobsOfSameColor:
                case MoveFailureReason.FlagRequiresNormalSource:
                case MoveFailureReason.FlagCaptureRequired:
                    return true;
                default:
                    return false;
            }
        }
    }
}
