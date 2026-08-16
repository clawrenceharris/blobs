using System.Collections.Generic;
using UnityEngine;

namespace Blobs.Content
{
    /// <summary>
    /// Authored visual footprint for a board surface. Coordinates are independent from optional
    /// gameplay TileState entries and are interpreted within the owning level's dimensions.
    /// </summary>
    [CreateAssetMenu(
        fileName = "BoardSurfaceLayout_New",
        menuName = "Blobs/Board Surface Layout")]
    public sealed class BoardSurfaceLayoutAsset : ScriptableObject
    {
        [SerializeField] private List<Vector2Int> occupiedCells = new();

        public IReadOnlyList<Vector2Int> OccupiedCells => occupiedCells;
        public bool HasOccupiedCells => occupiedCells.Count > 0;

        /// <summary>
        /// Validates that every authored cell is unique and lies within the owning board bounds.
        /// </summary>
        public bool TryValidate(int width, int height, out string error)
        {
            if (width <= 0 || height <= 0)
            {
                error = $"Board dimensions must be positive, received {width}x{height}.";
                return false;
            }

            var unique = new HashSet<Vector2Int>();
            for (int index = 0; index < occupiedCells.Count; index++)
            {
                Vector2Int cell = occupiedCells[index];
                if (cell.x < 0 || cell.y < 0 || cell.x >= width || cell.y >= height)
                {
                    error =
                        $"Cell {cell} at index {index} is outside board dimensions {width}x{height}.";
                    return false;
                }

                if (!unique.Add(cell))
                {
                    error = $"Cell {cell} is authored more than once.";
                    return false;
                }
            }

            error = null;
            return true;
        }
    }
}
