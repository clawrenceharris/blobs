using System.Collections;
using UnityEngine;
public abstract class Tile : BlobsGameObject
{

    [SerializeField] private TileVisuals visuals;
   
    public BlobColor Color
    {
        get; protected set;
    }

    public abstract Type.TileType TileType { get;}

    protected override void InitVisuals()
    {
        visualController = new TileVisualController();

    }
   

}
