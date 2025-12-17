
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class FlagBlob : Blob, IClearable
{
    public override BlobMergeBehavior Behavior => new FlagBlobBehavior(this);
    public override ColorRule Rule => new SameColorRule();
    public FlagBlob(BlobColor color, Vector2Int position) : base(BlobType.Flag, color, BlobSize.Normal, position)
    {
    }

    public override bool CanMergeWith(Blob targetBlob, MergePlan plan, BoardModel board)
    {
        // Flag Blob can be merged with another blob if there are exactly two clearable blobs on the board (the Flag Blob and source Blob).
        return base.CanMergeWith(targetBlob, plan, board) &&
        board
        .GetAllBlobs()
        .Where(b => b.ID != ID)
        .OfType<IClearable>()
        .Count() == 1;
           
    }
}