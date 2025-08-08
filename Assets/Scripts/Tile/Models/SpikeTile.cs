

using UnityEngine;

public class SpikeTile : Tile
{
    public BlobColor BlobColor { get; set; }

    public SpikeTile(Vector2Int position) : base(TileType.Spike, position)
    {
        
    }
    


}
