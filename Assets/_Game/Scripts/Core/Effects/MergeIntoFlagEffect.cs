namespace Blobs.Core
{
    public sealed class MergeIntoFlagEffect : IBoardEffect
    {
        public MergeIntoFlagEffect(
            BlobState source,
            BlobState flag)
        {
            Source = source;
            Flag = flag;
        }

        public BlobState Source { get; }
        public BlobState Flag { get; }

        public string SourceId => Source.Id;
        public string FlagId => Flag.Id;
        public GridPosition From => Source.Position;
        public GridPosition To => Flag.Position;

        public void Apply(BoardState board)
        {
            // The flag stays in place. Only the incoming source is consumed.
            board.RemoveBlob(Source.Id);
        }
    }
}