
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class FlagBlob : Blob
{
    public override BlobMergeBehavior Behavior => new FlagBlobBehavior(this);
    public override ColorRule Rule => new SameColorRule();
    public FlagBlob(BlobColor color, Vector2Int position) : base(BlobType.Flag, color, BlobSize.None, position)
    {
    }

}