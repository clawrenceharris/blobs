using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

/// <summary>
/// Effect: move a blob from one cell to another.
/// </summary>
public readonly struct MoveBlobEffect : IEffect
{
    public readonly string BlobId;
    public readonly Vector2Int From;
    public static readonly List<MoveBlobEffect> Moves = new();
    public readonly Vector2Int To;
    public readonly string MoveTag;

    public MoveBlobEffect(string blobId, Vector2Int from, Vector2Int to, string moveTag)
    {
        BlobId = blobId;
        From = from;
        To = to;
        MoveTag = moveTag;
        Moves.Add(this);

    }
}
