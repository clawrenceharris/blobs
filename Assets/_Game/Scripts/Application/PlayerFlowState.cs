namespace Blobs.Application
{
    /// <summary>
    /// Cross-scene handoff for the player flow. UI sets a pending level before loading Game;
    /// GameBootstrapper consumes it once on start. No Unity references.
    /// </summary>
    public static class PlayerFlowState
    {
        private static int? _pendingLevelIndex;

        public static void SetPendingLevelIndex(int levelIndex)
        {
            _pendingLevelIndex = levelIndex;
        }

        public static bool TryConsumePendingLevelIndex(out int levelIndex)
        {
            if (!_pendingLevelIndex.HasValue)
            {
                levelIndex = 0;
                return false;
            }

            levelIndex = _pendingLevelIndex.Value;
            _pendingLevelIndex = null;
            return true;
        }

        public static void Clear()
        {
            _pendingLevelIndex = null;
        }
    }
}
