
using UnityEngine;

public class TrailBlob : Blob, IMergable, IClearable
{
    public BlobColor TrailColor { get; set; }
    public override BlobMergeBehavior Behavior => new TrailBlobBehavior(this);

    public TrailBlob(BlobColor color, BlobSize size, BlobColor trailColor, Vector2Int position) : base(BlobType.Trail, color, size, position)
    {
        TrailColor = trailColor;
    }
}

