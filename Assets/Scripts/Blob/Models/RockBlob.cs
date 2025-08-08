using UnityEngine;

public class RockBlob : Blob
{
    public override BlobMergeBehavior Behavior => new RockBlobBehavior(this);

    public RockBlob(Vector2Int position) : base(BlobType.Rock, BlobColor.Blank, BlobSize.Normal, position)
    {
    }

}