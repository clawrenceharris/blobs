using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Type;
public class SpikeTile : Tile
{
    public override TileType TileType => TileType.Spike;

    public override void Init(Position position)
    {
        base.Init(position);

    }

   
}
