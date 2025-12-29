using System;
using UnityEngine;


[RequireComponent(typeof(LaserTileVisuals))]
public class LaserTile : Tile
{
    public BlobColor LaserColor { get; set; }
    public bool IsActive { get; private set; }
    public string LinkedLaserId { get; set; }
    public LaserTile LinkedLaser { get; set; } // Resolved at runtime
    public Vector2Int Direction => LinkedLaser != null
        ? NormalizeDirection(LinkedLaser.GridPosition - GridPosition)
        : Vector2Int.zero;

    private Vector2Int NormalizeDirection(Vector2Int vector)
    {
        int x = vector.x == 0 ? 0 : (vector.x > 0 ? 1 : -1);
        int y = vector.y == 0 ? 0 : (vector.y > 0 ? 1 : -1);
        return new Vector2Int(x, y);
    }
    public LaserTile(BlobColor color, string id, Vector2Int position) : base(TileType.Laser, position)
    {
        LaserColor = color;
        IsActive = true;
        ID = id;
    }

    public void Deactivate()
    {
        IsActive = false;
        
    }

    public void Toggle()
    {
        if (IsActive)
        {
            IsActive = false;

        }
        else
        {
            IsActive = true;
        }
    }
}