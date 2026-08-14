namespace Blobs.Core
{
    public interface IBoardEffect
    {
        IBoardEffect Apply(BoardState board);
    }
}
