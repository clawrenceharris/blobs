namespace Blobs.Core
{
    public sealed class MoveBlobEffect : IBoardEffect
    {
        public MoveBlobEffect(string blobId, GridPosition from, GridPosition to)
        {
            BlobId = blobId;
            From = from;
            To = to;
        }

        public string BlobId { get; }
        public GridPosition From { get; }
        public GridPosition To { get; }

        public IBoardEffect Apply(BoardState board)
        {
            board.MoveBlob(BlobId, To);
            return new MoveBlobEffect(BlobId, To, From);
        }
    }
}
