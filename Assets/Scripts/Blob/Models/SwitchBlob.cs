
using UnityEngine;

public class SwitchBlob : Blob
{
    public override BlobMergeBehavior Behavior => new SwitchBlobBehavior(this);
    public SwitchBlob(BlobColor color, Vector2Int position) : base(BlobType.Switch, color, BlobSize.Normal, position)
    {
    }
}
