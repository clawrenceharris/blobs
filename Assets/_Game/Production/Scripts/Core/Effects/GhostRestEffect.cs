using System.Collections.Generic;
using System.Linq;

namespace Blobs.Core
{
    /// <summary>
    /// Clears a ghost upon crossing a Sigil, retaining the contact position for presentation.
    /// </summary>
    public sealed class GhostRestEffect : IBoardEffect
    {
        /// <summary>
        /// Creates a ghost-clear event at the first Sigil on its return path.
        /// </summary>
        public GhostRestEffect(string ghostId, IReadOnlyList<GridPosition> path)
        {
            GhostId = ghostId;
            Path = path;
        }

        public string GhostId { get; }
        public IReadOnlyList<GridPosition> Path { get; }
        public GridPosition RestDestination => Path.Last();

        /// <inheritdoc />
        public void Apply(BoardState board)
        {
            board.RemoveBlob(GhostId);
        }
    }
}
