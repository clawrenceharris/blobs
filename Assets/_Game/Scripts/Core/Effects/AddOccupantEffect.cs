namespace Blobs.Core
{
    /// <summary>
    /// Adds an occupant to a blob.
    /// </summary>
    public sealed class AddOccupantEffect : IBoardEffect
    {
        /// <summary>
        /// Creates an add occupant effect.
        /// </summary>
        public AddOccupantEffect(string blobId, string occupantId)
        {
            BlobId = blobId;
            OccupantId = occupantId;
        }

        public AddOccupantEffect(BlobState blob, string occupantId)
            : this(blob.Id, occupantId)
        {
            Blob = blob;
        }

        public string BlobId { get; }
        public string OccupantId { get; }
        public BlobState Blob { get; }

        /// <inheritdoc />
        public void Apply(BoardState board)
        {
            board.SetOccupant(BlobId, OccupantId);
        }
    }
}
