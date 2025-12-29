using System;
using UnityEngine;

public class EnemyBlob : Blob, IClearable
{
    public override BlobMergeBehavior Behavior => new EnemyBlobBehavior(this);
    public bool IsWeakened { get; private set; }


    public EnemyBlob(BlobColor color, Vector2Int position) : base(BlobType.Enemy, color, BlobSize.Normal, position)
    {
    }

    public void Weaken()
    {
        
        IsWeakened = true;
    }
}