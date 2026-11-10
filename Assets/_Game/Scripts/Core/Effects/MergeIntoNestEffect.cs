namespace Blobs.Core
{
    /// <summary>
    /// Nest capture: the incoming monster is removed from the board and recorded as the
    /// nest's occupant. Presentation seats the retired view; Core only stores the id.
    /// </summary>
    public sealed class MergeIntoNestEffect : IBoardEffect
    {
        public MergeIntoNestEffect(
            string sourceId,
            string nestId,
            GridPosition at,
            GridPosition? from = null,
            BlobState sourceBlob = null,
            BlobState nestBlob = null)
        {
            SourceId = sourceId;
            NestId = nestId;
            At = at;
            From = from ?? at;
            SourceBlob = sourceBlob;
            NestBlob = nestBlob;
        }

        public MergeIntoNestEffect(BlobState source, BlobState nest)
            : this(source.Id, nest.Id, nest.Position, source.Position, source, nest)
        {
        }

        public string SourceId { get; }
        public string NestId { get; }
        public GridPosition At { get; }
        public GridPosition To => At;
        public GridPosition From { get; }

        /// <summary>Monster removed by the nest capture, captured before Apply.</summary>
        public BlobState SourceBlob { get; }

        /// <summary>Nest state at contact, before Apply recorded the occupant.</summary>
        public BlobState NestBlob { get; }

        public void Apply(BoardState board)
        {
            board.RemoveBlob(SourceId);
            board.SetOccupant(NestId, SourceId);
        }
    }
}
