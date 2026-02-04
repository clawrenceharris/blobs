namespace Blobs.Core.Merge
{
    public interface IMergeEvent
    {
        void Execute(BoardModel board);
        void Undo(BoardModel board);
    }
}