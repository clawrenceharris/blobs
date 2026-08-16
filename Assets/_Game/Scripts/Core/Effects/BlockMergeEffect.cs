using System;

namespace Blobs.Core
{
    public sealed class BlockMoveEffect : IBoardEffect
    {
        public BlockMoveEffect(string blobId, GridPosition from, GridPosition blockedPosition)
        {
            BlobId = blobId;
            From = from;
            BlockedPosition = blockedPosition;
        }



        public string BlobId { get; }
        public GridPosition From { get; }
        public GridPosition BlockedPosition { get; }

        /// <inheritdoc />
        public void Apply(BoardState board)
        {
            // Compute the direction of the attempted move
            var direction = new GridPosition(BlockedPosition.X - From.X, BlockedPosition.Y - From.Y);
            // Move back one cell from the blocked position in the opposite direction
            var newPosition = new GridPosition(BlockedPosition.X - direction.X, BlockedPosition.Y - direction.Y);
            board.MoveBlob(BlobId, newPosition);

        }
    }
}