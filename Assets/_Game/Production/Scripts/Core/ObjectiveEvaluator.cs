namespace Blobs.Core
{
    public static class ObjectiveEvaluator
    {
        public static bool IsComplete(
            BoardState board,
            LevelObjectiveDefinition objective = null)
        {
            if (board == null)
                return false;

            objective = objective ?? LevelObjectiveDefinition.ClearAllClearableBlobs;
            switch (objective.Type)
            {
                case LevelObjectiveType.ClearAllClearableBlobs:
                    return !board.HasAnyClearableBlobs();
                default:
                    throw new System.InvalidOperationException(
                        $"Unsupported level objective: {objective.Type}.");
            }
        }
    }
}
