using System.Collections.Generic;
using UnityEngine;

public readonly struct MergeEffect : IEffect
{
    public readonly string BlobToMoveId;
    public readonly Vector2Int From;
    public readonly Vector2Int To;
    public readonly string MergeTag;
    public  readonly string BlobToRemoveId;

    public MergeEffect(string blobToMoveId, string blobToRemoveId, Vector2Int from, Vector2Int to, string mergeTag = EffectTags.Merge)
    {
        
        BlobToRemoveId = blobToRemoveId;
        BlobToMoveId = blobToMoveId;
        From = from;
        To = to;
        MergeTag = mergeTag;
    }
}