using UnityEngine;


/// <summary>
/// Effect: change a blob's size (e.g. small+small -> big).
/// </summary>
public readonly struct ResizeBlobEffect : IEffect
{
    public readonly string BlobId;
    public readonly BlobSize From;
    public readonly BlobSize To;
    public readonly CellContext Cell;

    public ResizeBlobEffect(string blobId, BlobSize from, BlobSize to, CellContext cell)
    {
        BlobId = blobId;
        From = from;
        To = to;
        Cell = cell;
    }
}
