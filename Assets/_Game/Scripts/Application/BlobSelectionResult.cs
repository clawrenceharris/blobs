using Blobs.Core;

namespace Blobs.Application
{
    public sealed class BlobSelectionResult
    {
        public BlobSelectionResult(
            string selectedBlobId,
            bool moveAttempted,
            MoveResult moveResult)
        {
            SelectedBlobId = selectedBlobId;
            MoveAttempted = moveAttempted;
            MoveResult = moveResult;
        }

        public string SelectedBlobId { get; }
        public bool HasSelection => !string.IsNullOrEmpty(SelectedBlobId);
        public bool MoveAttempted { get; }
        public MoveResult MoveResult { get; }

        /// <summary>
        /// Selects a blob.
        /// </summary>
        /// <param name="blobId">The id of the blob to select.</param>
        /// <returns></returns>
        public static BlobSelectionResult Selected(string blobId)
        {
            return new BlobSelectionResult(blobId, false, null);
        }

        /// <summary>
        /// Clears the selection.
        /// </summary>
        /// <returns></returns>
        public static BlobSelectionResult Cleared()
        {
            return new BlobSelectionResult(null, false, null);
        }

        /// <summary>
        /// Moves the selected blob.
        /// </summary>
        /// <param name="result">The result of the move.</param>
        /// <returns></returns>
        public static BlobSelectionResult Move(MoveResult result)
        {
            return new BlobSelectionResult(null, true, result);
        }

        public override string ToString()
        {
            return $"BlobSelectionResult: SelectedBlobId = {SelectedBlobId}, MoveAttempted = {MoveAttempted}, MoveResult = {MoveResult}";
        }
    }
}
