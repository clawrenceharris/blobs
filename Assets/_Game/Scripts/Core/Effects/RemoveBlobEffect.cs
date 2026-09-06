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
        public RemoveBlobEffect(BlobState blob, GridPosition? at = null)
        {
            Blob = blob;
            At = at ?? blob.Position;
        }

        public BlobState Blob { get; }
        public string BlobId => Blob.Id;
        public GridPosition At { get; }

        /// <inheritdoc />
        public void Apply(BoardState board)
        {
            board.RemoveBlob(Blob.Id);
        }
    }
}
