namespace Blobs.Core
{
    /// <summary>
    /// Changes the size of a blob.
    /// </summary>
    public sealed class ChangeBlobSizeEffect : IBoardEffect
    {
        /// <summary>
        /// Creates a change blob size effect.
        /// </summary>
        public ChangeBlobSizeEffect(string blobId, BlobSize before, BlobSize after)
        {
            BlobId = blobId;
            Before = before;
            After = after;
        }

        public ChangeBlobSizeEffect(BlobState blob, BlobSize before, BlobSize after)
            : this(blob.Id, before, after)
        {
            Blob = blob;
        }

        public string BlobId { get; }
        public BlobSize Before { get; }
        public BlobSize After { get; }
        public BlobState Blob { get; }

        /// <inheritdoc />
        public void Apply(BoardState board)
        {
            board.SetBlobSize(BlobId, After);
        }
    }
}
