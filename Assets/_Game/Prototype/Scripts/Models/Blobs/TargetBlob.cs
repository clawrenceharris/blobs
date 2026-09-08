
using System.Linq;
using UnityEngine;

public class TargetBlob : Blob
{
    public override IMergeRule MatchRule => new TargetColorMergeRule();
    public TargetBlob(BlobColor color, Vector2Int position, string id = null) : base(BlobType.Target, color, BlobSize.None, position, id) { }
}