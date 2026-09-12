using System.Collections.Generic;
using System.Linq;

namespace Blobs.Core
{
    /// <summary>
    /// Clears a ghost upon crossing a Grave, retaining the contact position for presentation.
    /// </summary>
    public sealed class GhostRestEffect : IBoardEffect
    {
        /// <summary>
        /// Creates a ghost-clear event at the first Grave on its return path.
        /// </summary>
        public GhostRestEffect(string ghostId, IReadOnlyList<GridPosition> path)
        {
            GhostId = ghostId;
            Path = path;
        }

        public string GhostId { get; }
        public IReadOnlyList<GridPosition> Path { get; }
        public GridPosition RestDestination => Path.Last();
        public string LandingBlobId { get; private set; }
        public BlobState GhostBlob { get; private set; }
        public BlobState LandingBlob { get; private set; }

        /// <inheritdoc />
        public void Apply(BoardState board)
        {
            GhostBlob = board.GetBlob(GhostId);
            board.RemoveBlob(GhostId);
            LandingBlob = board.GetBlobAt(Path.Last());
            LandingBlobId = LandingBlob?.Id;
            if (LandingBlobId != null)
                board.RemoveBlob(LandingBlobId);
        }
    }
}
