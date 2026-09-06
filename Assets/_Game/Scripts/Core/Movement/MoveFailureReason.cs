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
        BlockedPath,
        UnsupportedInteraction,

        NormalMergeRequiresDifferentColors,
        FlagRequiresMatchingColor,
        FlagRequiresNoOtherBlobs
    }

    public static class MoveFailureReasonExtensions
    {
        public static bool ShouldShowFeedback(this MoveFailureReason reason)
        {
            return reason switch
            {
                MoveFailureReason.SourceCannotMove => true,
                MoveFailureReason.SameBlob => false,
                MoveFailureReason.NotAligned => false,
                MoveFailureReason.BlockedPath => true,
                MoveFailureReason.UnsupportedInteraction => true,
                MoveFailureReason.NormalMergeRequiresDifferentColors => true,
                MoveFailureReason.FlagRequiresMatchingColor => true,
                MoveFailureReason.FlagRequiresNoOtherBlobs => true,
                MoveFailureReason.None => false,
                MoveFailureReason.SourceOrTargetMissing => false,
                MoveFailureReason.MoveTimeout => true,
                _ => false,
            };
        }
    }
}