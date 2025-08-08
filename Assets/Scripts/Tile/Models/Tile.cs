
using System;
using UnityEngine;

public abstract class Tile : IBlobModel
{
    public string ID { get; protected set; }
    public TileType TileType { get; protected set; }
    public Vector2Int GridPosition { get; set; }
    public virtual TileMergeBehavior Behavior => new(this);
    public Tile(TileType type,Vector2Int position)
    {
        ID = Guid.NewGuid().ToString();
        TileType = type;
        GridPosition = position;
    }



}

