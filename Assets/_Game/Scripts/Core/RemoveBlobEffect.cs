namespace Blobs.Core
{
    public sealed class RemoveBlobEffect : IBoardEffect
    {
        public RemoveBlobEffect(BlobState blob)
        {
            Blob = blob;
        }

        public BlobState Blob { get; }
        public string BlobId => Blob.Id;
        public GridPosition At => Blob.Position;

        public IBoardEffect Apply(BoardState board)
        {
            board.RemoveBlob(Blob.Id);
            return new SpawnBlobEffect(Blob);
        }
    }
}
