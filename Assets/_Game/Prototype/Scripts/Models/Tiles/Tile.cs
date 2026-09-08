
using System;
using UnityEngine;

public abstract class Tile : ITileModel
{
    public string ID { get; protected set; }
    public TileType Type { get; protected set; }
    public Vector2Int GridPosition { get; set; }
    public Tile(TileType type,Vector2Int position)
    {
        ID = Guid.NewGuid().ToString();
        Type = type;
        GridPosition = position;
    }



}

