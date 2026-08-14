namespace Blobs.Core
{
    public static class ObjectiveEvaluator
    {
        public static bool IsComplete(BoardState board)
        {
            return board != null && board.Blobs.Count == 1; // TODO: Remove this once we have a proper objective evaluator
            // return board != null && !board.HasAnyClearableBlobs();
        }
    }
}
