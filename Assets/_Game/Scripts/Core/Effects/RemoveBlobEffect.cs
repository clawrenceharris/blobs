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
        public RemoveBlobEffect(BlobState blob)
        {
            Blob = blob;
        }

        public BlobState Blob { get; }
        public string BlobId => Blob.Id;
        public GridPosition At => Blob.Position;

        /// <inheritdoc />
        public void Apply(BoardState board)
        {
            board.RemoveBlob(Blob.Id);
        }
    }
}
