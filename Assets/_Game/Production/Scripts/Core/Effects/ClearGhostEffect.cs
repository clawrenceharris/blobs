namespace Blobs.Core
{
    /// <summary>
    /// Clears a ghost upon crossing a Sigil, retaining the contact position for presentation.
    /// </summary>
    public sealed class ClearGhostEffect : IBoardEffect
    {
        /// <summary>
        /// Creates a ghost-clear event at the first Sigil on its return path.
        /// </summary>
        public ClearGhostEffect(string ghostId, GridPosition at)
        {
            GhostId = ghostId;
            At = at;
        }

        public string GhostId { get; }
        public GridPosition At { get; }

        /// <inheritdoc />
        public void Apply(BoardState board)
        {
            board.RemoveBlob(GhostId);
        }
    }
}
