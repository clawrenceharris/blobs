
using UnityEngine;

public class SwitchBlob : Blob
{
    public SwitchBlob(BlobColor color, Vector2Int position) : base(BlobType.Switch, color, BlobSize.Normal, position) { }
    public SwitchBlob(BlobColor color, Vector2Int position, string id) : base(BlobType.Switch, color, BlobSize.Normal, position, id) { }
}
