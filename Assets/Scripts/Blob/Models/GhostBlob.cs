
using UnityEngine;

public class GhostBlob : Blob, IClearable
{
    public override BlobMergeBehavior Behavior => new GhostBlobBehavior(this);
    public GhostBlob(Vector2Int position) : base(BlobType.Ghost, BlobColor.Blank, BlobSize.Normal, position)
    {
        
    }
}
