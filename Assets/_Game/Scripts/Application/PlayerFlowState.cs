namespace Blobs.Application
{
    /// <summary>
    /// Cross-scene handoff for the player flow. UI sets a pending level before loading Game;
    /// GameBootstrapper consumes it once on start. No Unity references.
    /// </summary>
    public static class PlayerFlowState
    {
        private static int? _pendingLevelIndex;
        private static int? _pendingWorldIndex;
        public static void SetPendingLevelIndex(int levelIndex)
        {
            _pendingLevelIndex = levelIndex;
        }

        public static void SetPendingWorldIndex(int worldIndex)
        {
            _pendingWorldIndex = worldIndex;
        }

        public static bool TryConsumePendingWorldIndex(out int worldIndex)
        {
            if (!_pendingWorldIndex.HasValue)
            {
                worldIndex = 0;
                return false;
            }

            worldIndex = _pendingWorldIndex.Value;
            _pendingWorldIndex = null;
            return true;
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
