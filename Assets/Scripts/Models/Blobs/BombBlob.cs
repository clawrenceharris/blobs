
using UnityEngine;

public class BombBlob : Blob, IMovable, IClearable
{
    public BombBlob(Vector2Int position) : base(BlobType.Bomb, BlobColor.Blank, BlobSize.Normal, position) { }
    public BombBlob(Vector2Int position, string id) : base(BlobType.Bomb, BlobColor.Blank, BlobSize.Normal, position, id) { }
}