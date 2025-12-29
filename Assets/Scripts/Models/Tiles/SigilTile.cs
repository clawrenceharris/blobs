

using UnityEngine;

public class SigilTile : Tile
{
    public override TileMergeBehavior Behavior => new SigilTileBehavior(this);

    public SigilTile(Vector2Int position) : base(TileType.Sigil, position)
    {

    }



}


