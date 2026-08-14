namespace Blobs.Core
{
    /// <summary>
    /// Adds a new blob to the board. This supports future mechanics such as Trail blobs and cascades.
    /// </summary>
    public sealed class SpawnBlobEffect : IBoardEffect
    {
        /// <summary>
        /// Creates a spawn effect for the supplied blob state.
        /// </summary>
        public SpawnBlobEffect(BlobState blob)
        {
            Blob = blob;
        }

        public BlobState Blob { get; }
        public string BlobId => Blob.Id;
        public GridPosition At => Blob.Position;

        /// <inheritdoc />
        public void Apply(BoardState board)
        {
            board.AddBlob(Blob);
        }
    }
}
