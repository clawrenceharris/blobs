using Blobs.Core;

namespace Blobs.Application
{

    public enum BlobSelectionResultType
    {
        Selected,
        Cleared,
        MoveAttempted,
        MoveFailed
    }
    /// <summary>
    /// Describes the outcome of selecting a grid position through <see cref="GameSession"/>.
    /// A selection can choose a source, clear the current source, or attempt a move.
    /// </summary>
    public sealed class BlobSelectionResult
    {
        /// <summary>
        /// Creates a selection result.
        /// </summary>
        public BlobSelectionResult(
            string sourceId,
            string targetId,
            bool moveAttempted,
            MoveResult moveResult,
            bool targetSelected = true)
        {
            SourceId = sourceId;
            TargetId = targetId;
            MoveAttempted = moveAttempted;
            MoveResult = moveResult;
            TargetSelected = targetSelected && !string.IsNullOrEmpty(targetId);
        }

        /// <summary>
        /// The currently selected source blob id, if selection is active.
        /// </summary>
        public string SourceId { get; }

        /// <summary>
        /// The aimed target blob id, if any. Illegal aims may still report a target while
        /// <see cref="TargetSelected"/> is false so aim feedback can use a different channel.
        /// </summary>
        public string TargetId { get; }

        /// <summary>
        /// True when <see cref="TargetId"/> is part of the active selection (valid aim).
        /// Invalid aims keep the source selected and leave target highlight off.
        /// </summary>
        public bool TargetSelected { get; }

        /// <summary>
        /// True when the result leaves a source blob or target blob selected.
        /// </summary>
        public bool HasSelection =>
            !string.IsNullOrEmpty(SourceId) || TargetSelected;


        /// <summary>
        /// True when the result leaves the specified blob selected.
        /// </summary>
        public bool IsSelected(string id) =>
            id == SourceId || (TargetSelected && id == TargetId);

        /// <summary>
        /// True when this selection completed a source-to-target move attempt.
        /// </summary>
        public bool MoveAttempted { get; }

        /// <summary>
        /// Result of the attempted move, or null when this selection did not attempt a move.
        /// </summary>
        public MoveResult MoveResult { get; }

        /// <summary>
        /// Creates a result indicating that the supplied blob is now the selected source.
        /// Pass <paramref name="targetSelected"/> false when a target is under aim but should
        /// not join the selection highlight (for example an illegal drag).
        /// </summary>
        public static BlobSelectionResult Selected(
            string sourceBlobId,
            string targetBlobId,
            bool targetSelected = true)
        {
            return new BlobSelectionResult(
                sourceBlobId,
                targetBlobId,
                false,
                null,
                targetSelected);
        }

        /// <summary>
        /// Creates a result indicating that no source is selected.
        /// </summary>
        public static BlobSelectionResult Cleared()
        {
            return new BlobSelectionResult(null, null, false, null);
        }

        /// <summary>
        /// Creates a result for a completed move attempt.
        /// </summary>
        public static BlobSelectionResult Move(MoveResult result)
        {
            return new BlobSelectionResult(result.SourceBlobId, result.TargetBlobId, true, result);
        }
        public static BlobSelectionResult Rejected(MoveFailureReason reason)
        {
            return new BlobSelectionResult(null, null, false, MoveResult.Failed(null, null, reason));
        }

        public override string ToString()
        {
            return $"BlobSelectionResult: SourceBlobId = {SourceId}, TargetBlobId = {TargetId}, TargetSelected = {TargetSelected}, MoveAttempted = {MoveAttempted}, MoveResult = {MoveResult}";
        }
    }
}
