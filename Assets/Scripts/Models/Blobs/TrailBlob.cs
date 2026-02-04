
using UnityEngine;

public class TrailBlob : Blob, IMovable, IClearable
{
    public BlobColor TrailColor { get; set; }

    public TrailBlob(BlobColor color, BlobSize size, BlobColor trailColor, Vector2Int position) : base(BlobType.Trail, color, size, position)
    {
        TrailColor = trailColor;
    }
}

