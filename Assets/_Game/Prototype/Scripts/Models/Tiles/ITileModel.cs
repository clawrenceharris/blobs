using UnityEngine;

public interface ITileModel
{
    string ID { get; }
    TileType Type { get; }
    Vector2Int GridPosition { get; set; }
    
}