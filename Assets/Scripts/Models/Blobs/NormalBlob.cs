
using UnityEngine;

public class NormalBlob : Blob, IMergable, IClearable
{
    public NormalBlob(BlobColor color, BlobSize size, Vector2Int position) : base(BlobType.Normal, color, size, position)
    {
    }
}
