namespace Blobs.Core
{
    /// <summary>Consumes the incoming source while preserving the ghost at the collision site.</summary>
    public sealed class MergeIntoGhostEffect : IBoardEffect
    {
        public MergeIntoGhostEffect(string sourceId, string ghostId, GridPosition at,
            GridPosition finalPosition, bool isClear)
        {
            SourceId = sourceId;
            GhostId = ghostId;
            At = at;
            FinalPosition = finalPosition;
            IsClear = isClear;
        }

        public string SourceId { get; }
        public string GhostId { get; }
        public GridPosition At { get; }
        /// <summary>Return destination, shortened to the first Grave when clearing.</summary>
        public GridPosition FinalPosition { get; }
        public bool IsClear { get; }

        public void Apply(BoardState board) => board.RemoveBlob(SourceId);
    }
}
