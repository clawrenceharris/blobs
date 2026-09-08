namespace Blobs.Core
{
    public sealed class MergeIntoFlagEffect : IBoardEffect
    {
        public MergeIntoFlagEffect(
            string sourceId,
            string flagId,

            GridPosition at, GridPosition? from = null)
        {
            SourceId = sourceId;
            FlagId = flagId;
            At = at;
            From = from ?? at;
        }

        public MergeIntoFlagEffect(BlobState source, BlobState flag)
            : this(source.Id, flag.Id, flag.Position, source.Position) { }

        public string SourceId { get; }
        public string FlagId { get; }
        public GridPosition At { get; }
        public GridPosition To => At;
        public GridPosition From { get; }
        public void Apply(BoardState board)
        {
            // The flag stays in place. Only the incoming source is consumed.
            board.RemoveBlob(SourceId);
        }
    }
}