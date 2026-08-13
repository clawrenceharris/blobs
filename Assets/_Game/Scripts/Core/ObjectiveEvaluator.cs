namespace Blobs.Core
{
    public static class ObjectiveEvaluator
    {
        public static bool IsComplete(BoardState board)
        {
            return board != null && !board.HasAnyClearableBlobs();
        }
    }
}
