using System;
using UnityEngine;

public class EnemyBlob : Blob, IClearable
{
    public bool IsWeakened { get; private set; }

    public EnemyBlob(BlobColor color, Vector2Int position) : base(BlobType.Enemy, color, BlobSize.Normal, position) { }
    public EnemyBlob(BlobColor color, Vector2Int position, string id) : base(BlobType.Enemy, color, BlobSize.Normal, position, id) { }

    public void Weaken()
    {
        
        IsWeakened = true;
    }
}