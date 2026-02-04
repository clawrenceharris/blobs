namespace Blobs.Core.Merge
{
public readonly struct PathResolveResult
{
    public readonly bool Success;
    public readonly MergeFailReason FailReason;
    public readonly ResolvedPath Path;

    public PathResolveResult(bool success, MergeFailReason reason, ResolvedPath path)
    {
        Success = success;
        FailReason = reason;
        Path = path;
    }
}
}