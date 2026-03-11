
using UnityEngine;

public class NormalBlob : Blob, IMovable, IClearable
{
    public NormalBlob(BlobColor color, BlobSize size, Vector2Int position) : base(BlobType.Normal, color, size, position) { }
    public NormalBlob(BlobColor color, BlobSize size, Vector2Int position, string id) : base(BlobType.Normal, color, size, position, id) { }
}
