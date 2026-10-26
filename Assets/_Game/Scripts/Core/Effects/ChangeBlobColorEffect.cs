namespace Blobs.Core
{
    /// <summary>
    /// Changes the size of a blob.
    /// </summary>
    public sealed class ChangeBlobColorEffect : IBoardEffect
    {
        /// <summary>
        /// Creates a change blob size effect.
        /// </summary>
        public ChangeBlobColorEffect(string blobId, BlobColor before, BlobColor after)
        {
            BlobId = blobId;
            Before = before;
            After = after;
        }

        public ChangeBlobColorEffect(BlobState blob, BlobColor before, BlobColor after)
            : this(blob.Id, before, after)
        {
            Blob = blob;
        }

        public string BlobId { get; }
        public BlobColor Before { get; }
        public BlobColor After { get; }
        public BlobState Blob { get; }

        /// <inheritdoc />
        public void Apply(BoardState board)
        {
            board.SetBlobColor(BlobId, After);
        }
    }
}
