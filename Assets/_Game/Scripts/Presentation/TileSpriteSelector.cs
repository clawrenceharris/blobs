using System;
using UnityEngine;


/// <summary>
/// Class to determine which tile sprite (0-15) to use based on neighboring tiles.
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
    public static int GetSpriteIndex(Vector2Int gridPosition, Func<Vector2Int, bool> hasLeft, Func<Vector2Int, bool> hasTop, Func<Vector2Int, bool> hasRight, Func<Vector2Int, bool> hasBottom)
    {


        // Check for neighbors in 4 cardinal directions
        bool top = hasTop(gridPosition + Vector2Int.up);
        bool bottom = hasBottom(gridPosition + Vector2Int.down);
        bool left = hasLeft(gridPosition + Vector2Int.left);
        bool right = hasRight(gridPosition + Vector2Int.right);

        // 0: No neighbors - isolated tile
        if (!top && !bottom && !left && !right) return 0;

        // 1-4: Single neighbor
        if (left && !top && !right && !bottom) return 1;
        if (top && !left && !right && !bottom) return 2;
        if (right && !left && !top && !bottom) return 3;
        if (bottom && !left && !top && !right) return 4;
        // 5-6: Two neighbors (opposite sides - edge exposed on two sides)
        if (left && right && !top && !bottom) return 5;   // Horizontal line
        if (top && bottom && !left && !right) return 6;  // Vertical line

        return 0;
    }

}

