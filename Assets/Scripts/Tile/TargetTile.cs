using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Type;
public class TargetTile : Tile
{
    public override TileType TileType => TileType.Target;

    protected override void InitVisuals()
    {
        TargetTileVisualController visualController = new TargetTileVisualController();
        visualController.Init(this);
    }
}

    