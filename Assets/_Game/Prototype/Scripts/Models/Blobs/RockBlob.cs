using UnityEngine;

public class RockBlob : Blob
{
    public override IMoveBehavior MoveBehavior => new RockBlobMoveBehavior();
    public RockBlob(Vector2Int position, string id = null) : base(BlobType.Rock, BlobColor.Blank, BlobSize.Normal, position, id) { }

}