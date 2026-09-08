using UnityEngine;

/// <summary>
/// Effect: remove a blob from the board at the given position.
/// </summary>
public readonly struct RemoveBlobEffect : IEffect
{
    public readonly string BlobId;
    public readonly string CauseTag;

    public readonly Vector2Int At;

    public RemoveBlobEffect(string blobId, Vector2Int at, string causeTag)
    {
        BlobId = blobId;
        CauseTag = causeTag;
        At = at;
    }
}
