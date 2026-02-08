
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class FlagBlob : Blob
{
    public FlagBlob(BlobColor color, Vector2Int position) : base(BlobType.Flag, color, BlobSize.None, position)
    {
    }

}