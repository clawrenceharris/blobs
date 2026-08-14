namespace Blobs.Core
{
    /// <summary>
    /// Moves an existing blob from one logical grid position to another.
    /// Presentation uses the source and target positions to animate the source blob into the target cell.
    /// </summary>
    public sealed class MoveBlobEffect : IBoardEffect
    {
        /// <summary>
        /// Creates an effect for moving an existing blob.
        /// </summary>
        public MoveBlobEffect(string blobId, GridPosition from, GridPosition to)
        {
            BlobId = blobId;
            From = from;
            To = to;
        }

        public string BlobId { get; }
        public GridPosition From { get; }
        public GridPosition To { get; }

        /// <inheritdoc />
        public void Apply(BoardState board)
        {
            board.MoveBlob(BlobId, To);
        }
    }
}
