
using UnityEngine;

public class BombBlob : Blob, IMergable, IClearable
{
    public override BlobMergeBehavior Behavior => new BombBlobBehavior(this);
    public BombBlob(Vector2Int position) : base(BlobType.Bomb, BlobColor.Blank, BlobSize.Normal, position)
    {

    }
   
}