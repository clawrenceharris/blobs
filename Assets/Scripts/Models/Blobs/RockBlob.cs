using UnityEngine;

public class RockBlob : Blob
{
    public RockBlob(Vector2Int position) : base(BlobType.Rock, BlobColor.Blank, BlobSize.Normal, position)
    {
    }

}