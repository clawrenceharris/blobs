using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LaserBeamPresenter : MonoBehaviour
{
    private readonly List<GameObject> _activeBeams = new();
    
   
    public void Setup(BoardPresenter board)
    {
        IEnumerable<LaserTile> lasers = board.BoardModel.GetAllTiles().OfType<LaserTile>();
        foreach (LaserTile laser in lasers)
        {
            laser.LinkedLaser = (LaserTile)board.BoardModel.GetTile(laser.LinkedLaserId);

            Vector2Int dir = laser.Direction;
            LaserTileView view = (LaserTileView)board.GetTileView(laser.ID);
            view.SetRotationFromDirection(dir);
            SpawnBeamBetween(view, laser.GridPosition, laser.LinkedLaser.GridPosition, laser.LaserColor);

        }
    }
    /// <summary>
    /// Clears all laser beams currently on the board.
    /// </summary>
    public void ClearAllBeams()
    {
        foreach (GameObject beam in _activeBeams)
        {
            if (beam != null)
                Object.Destroy(beam);
        }
        _activeBeams.Clear();
    }

    /// <summary>
    /// Spawns a beam between two tiles in world space, stretching and rotating the prefab.
    /// </summary>
    /// <param name="from">Grid position of first laser tile.</param>
    /// <param name="to">Grid position of second laser tile.</param>
    /// <param name="color">The beam's color.</param>
    public void SpawnBeamBetween(LaserTileView laser, Vector2Int from, Vector2Int to, BlobColor color)
    {

        // Check if they are on the same row or column
        bool sameRow = from.y == to.y;
        bool sameCol = from.x == to.x;

        if (!sameRow && !sameCol)
        {
            Debug.LogWarning("Laser tiles must be aligned horizontally or vertically.");
            return;
        }

        // Calculate direction and distance
        Vector2Int direction = sameRow
            ? (to.x > from.x ? Vector2Int.right : Vector2Int.left)
            : (to.y > from.y ? Vector2Int.up : Vector2Int.down);

        // Start at tileA and step toward tileB
        Vector2Int currentPos = from + direction;
        while (currentPos != to)
        {
            GameObject beam = SpawnLaserBeamAt(laser, currentPos, direction);
            _activeBeams.Add(beam);
            currentPos += direction;
        }
    }

    private static GameObject SpawnLaserBeamAt(LaserTileView laser, Vector2Int gridPos, Vector2Int direction)
    {
        Vector3 worldPos = new Vector3(gridPos.x, gridPos.y, 0) * BoardPresenter.TileSize;
        GameObject beam = Object.Instantiate(laser.GetVisuals<LaserTileVisuals>().LaserBeam, worldPos, Quaternion.identity, laser.transform);
        beam.name = "Laser Beam";
        // Rotate beam based on direction
        if (direction == Vector2Int.right || direction == Vector2Int.left)
            beam.transform.rotation = Quaternion.Euler(0, 0, 90);
        else
            beam.transform.rotation = Quaternion.identity;
        return beam;
}

    
}