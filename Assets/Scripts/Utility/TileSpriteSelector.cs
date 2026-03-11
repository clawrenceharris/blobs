using UnityEngine;

namespace Blobs.Utilities
{
    /// <summary>
    /// Utility class to determine which tile sprite (0-15) to use based on neighboring tiles.
    /// All 16 configurations (2^4 cardinal directions) are supported for visual depth in top-down view.
    /// </summary>
    public static class TileSpriteSelector
    {
        /// <summary>
        /// Gets the sprite index (0-15) for a tile based on its neighbors.
        /// </summary>
        /// <param name="boardModel">The board model to query for neighbors</param>
        /// <param name="gridPosition">The grid position of the tile</param>
        /// <returns>Sprite index from 0-15</returns>
        public static int GetSpriteIndex(BoardModel boardModel, Vector2Int gridPosition)
        {
            if (boardModel == null)
            {
                Debug.LogWarning("[TileSpriteSelector] BoardModel is null, returning default sprite index 0");
                return 0;
            }

            // Check for neighbors in 4 cardinal directions
            bool hasTop = HasNeighbor(boardModel, gridPosition, Vector2Int.up);
            bool hasBottom = HasNeighbor(boardModel, gridPosition, Vector2Int.down);
            bool hasLeft = HasNeighbor(boardModel, gridPosition, Vector2Int.left);
            bool hasRight = HasNeighbor(boardModel, gridPosition, Vector2Int.right);

            // 0: No neighbors - isolated tile
            if (!hasTop && !hasBottom && !hasLeft && !hasRight) return 0;

            // 1-4: Single neighbor
            if (hasLeft && !hasTop && !hasRight && !hasBottom) return 1;
            if (hasTop && !hasLeft && !hasRight && !hasBottom) return 2;
            if (hasRight && !hasLeft && !hasTop && !hasBottom) return 3;
            if (hasBottom && !hasLeft && !hasTop && !hasRight) return 4;

            // 5-8: Two neighbors (corners)
            if (hasLeft && hasTop && !hasRight && !hasBottom) return 5;
            if (hasTop && hasRight && !hasLeft && !hasBottom) return 6;
            if (hasRight && hasBottom && !hasLeft && !hasTop) return 7;
            if (hasBottom && hasLeft && !hasTop && !hasRight) return 8;

            // 9-10: Two neighbors (opposite sides - edge exposed on two sides)
            if (hasLeft && hasRight && !hasTop && !hasBottom) return 9;   // Horizontal line
            if (hasTop && hasBottom && !hasLeft && !hasRight) return 10;  // Vertical line

            // 11-14: Three neighbors (edge exposed on one side)
            if (hasLeft && hasTop && hasRight && !hasBottom) return 11;   // Missing bottom
            if (hasLeft && hasTop && hasBottom && !hasRight) return 12;   // Missing right
            if (hasLeft && hasRight && hasBottom && !hasTop) return 13;   // Missing top
            if (hasTop && hasRight && hasBottom && !hasLeft) return 14;   // Missing left

            // 15: All four neighbors - fully surrounded
            return 15;
        }

        /// <summary>
        /// Checks if there is a neighbor tile at the specified offset position.
        /// </summary>
        private static bool HasNeighbor(BoardModel boardModel, Vector2Int position, Vector2Int offset)
        {
            Vector2Int neighborPos = position + offset;
            
            // Check if position is valid
            if (!boardModel.IsValidPosition(neighborPos))
            {
                return false;
            }

            // Check if there's a tile at that position
            Tile neighbor = boardModel.GetTileAt(neighborPos);
            return neighbor != null;
        }
    }
}
