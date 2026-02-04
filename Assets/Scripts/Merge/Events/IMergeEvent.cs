namespace Blobs.Core.Merge
{
    public interface IMergeEvent
    {
        void Execute(IBoardPresenter board);
        void Undo(IBoardPresenter board);
    }
}