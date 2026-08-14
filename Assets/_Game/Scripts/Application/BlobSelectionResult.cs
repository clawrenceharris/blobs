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

        public static BlobSelectionResult Selected(string blobId)
        {
            return new BlobSelectionResult(blobId, false, null);
        }

        public static BlobSelectionResult Cleared()
        {
            return new BlobSelectionResult(null, false, null);
        }

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
