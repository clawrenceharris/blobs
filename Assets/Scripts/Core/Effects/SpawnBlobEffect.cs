using UnityEngine;

/// <summary>
/// Effect: spawn a blob at the given position. Data must include deterministic Id for undo.
/// </summary>
public readonly struct SpawnBlobEffect : IEffect
{
    public readonly Blob Blob;
    public readonly Vector2Int At;

    public SpawnBlobEffect(Blob blob, Vector2Int at)
    {
        Blob = blob;
        At = at;
    }

}
