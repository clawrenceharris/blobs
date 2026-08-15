using Blobs.Core;

namespace Blobs.Application
{
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
            string selectedBlobId,
            bool moveAttempted,
            MoveResult moveResult)
        {
            SelectedBlobId = selectedBlobId;
            MoveAttempted = moveAttempted;
            MoveResult = moveResult;
        }

        /// <summary>
        /// The currently selected source blob id, if selection is active.
        /// </summary>
        public string SelectedBlobId { get; }

        /// <summary>
        /// True when the result leaves a source blob selected.
        /// </summary>
        public bool HasSelection => !string.IsNullOrEmpty(SelectedBlobId);

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
        /// </summary>
        public static BlobSelectionResult Selected(string blobId)
        {
            return new BlobSelectionResult(blobId, false, null);
        }

        /// <summary>
        /// Creates a result indicating that no source is selected.
        /// </summary>
        public static BlobSelectionResult Cleared()
        {
            return new BlobSelectionResult(null, false, null);
        }

        /// <summary>
        /// Creates a result for a completed move attempt.
        /// </summary>
        public static BlobSelectionResult Move(MoveResult result)
        {
            return new BlobSelectionResult(null, true, result);
        }
        public static BlobSelectionResult Rejected(MoveFailureReason reason)
        {
            return new BlobSelectionResult(null, false, MoveResult.Failed(reason));
        }

        public override string ToString()
        {
            return $"BlobSelectionResult: SelectedBlobId = {SelectedBlobId}, MoveAttempted = {MoveAttempted}, MoveResult = {MoveResult}";
        }
    }
}
