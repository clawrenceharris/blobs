namespace Blobs.Core
{
    /// <summary>
    /// Removes a blob from the board. In a normal merge this is currently used for the target blob.
    /// </summary>
    public sealed class RemoveBlobEffect : IBoardEffect
    {
        /// <summary>
        /// Creates a removal effect that preserves the removed blob data for presentation and tooling.
        /// </summary>
        public RemoveBlobEffect(string blobId, GridPosition at)
        {
            BlobId = blobId;
            At = at;
        }

        public RemoveBlobEffect(BlobState blob, GridPosition? at = null)
            : this(blob.Id, at ?? blob.Position) { }

        public string BlobId { get; }
        public GridPosition At { get; }

        /// <inheritdoc />
        public void Apply(BoardState board)
        {
            board.RemoveBlob(BlobId);
        }
    }
}
