namespace Blobs.Core.Merge
{
    public interface IPathResolver
    {
        PathResolveResult Resolve(BoardModel board, MergeRequest request);
    }
}