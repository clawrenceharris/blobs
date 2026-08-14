namespace Blobs.Core
{
    public sealed class SpawnBlobEffect : IBoardEffect
    {
        public SpawnBlobEffect(BlobState blob)
        {
            Blob = blob;
        }

        public BlobState Blob { get; }
        public string BlobId => Blob.Id;
        public GridPosition At => Blob.Position;

        public IBoardEffect Apply(BoardState board)
        {
            board.AddBlob(Blob);
            return new RemoveBlobEffect(Blob);
        }
    }
}
