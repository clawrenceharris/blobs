
using UnityEngine;

public class GhostBlob : Blob, IClearable
{
    public GhostBlob(Vector2Int position) : base(BlobType.Ghost, BlobColor.Blank, BlobSize.None, position)
    {
        
    }
}
