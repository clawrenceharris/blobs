using Blobs.Application;
using Blobs.Content;

namespace Blobs.UI
{
    /// <summary>
    /// Skeleton player-flow navigation between Home, Levels, and Game scenes.
    /// </summary>
    public static class PlayerFlow
    {
        public const string HomeScene = "Home";
        public const string LevelsScene = "Levels";
        public const string GameScene = "Game";
        public const string WorldsScene = "Worlds";

        public static WorldDefinitionAsset ActiveWorld { get; private set; }
        public static int ActiveWorldIndex { get; private set; }

        // Statics survive Play mode when domain reload is disabled; drop any stale level request.
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetPendingLevel()
        {
            PlayerFlowState.Clear();
            ActiveWorld = null;
            ActiveWorldIndex = 0;
        }

        public static void GoHome() => GoHome(null);

        public static void GoHome(BlobsterTransitionStyle transitionStyle)
        {
            PlayerFlowState.Clear();
            ActiveWorld = null;
            ActiveWorldIndex = 0;
            BlobsterSceneTransition.LoadScene(HomeScene, transitionStyle);
        }

        public static void GoWorlds() => GoWorlds(null);

        public static void GoWorlds(BlobsterTransitionStyle transitionStyle)
        {
            WorldTransitionPresentation presentation = ActiveWorld?.Transition;
            PlayerFlowState.Clear();
            BlobsterSceneTransition.LoadScene(WorldsScene, transitionStyle, presentation);
        }

        public static void GoLevels() => GoLevels(null);

        public static void GoLevels(BlobsterTransitionStyle transitionStyle)
        {
            PlayerFlowState.SetPendingWorldIndex(ActiveWorldIndex);
            BlobsterSceneTransition.LoadScene(LevelsScene, transitionStyle, ActiveWorld?.Transition);
        }

        public static void GoLevels(int worldIndex, WorldDefinitionAsset world)
        {
            ActiveWorldIndex = worldIndex;
            ActiveWorld = world;
            PlayerFlowState.SetPendingWorldIndex(worldIndex);
            BlobsterSceneTransition.LoadScene(LevelsScene, presentation: world?.Transition);
        }

        public static void GoGame(int levelIndex) => GoGame(levelIndex, null);

        public static void GoGame(int levelIndex, BlobsterTransitionStyle transitionStyle)
        {
            PlayerFlowState.SetPendingLevelIndex(levelIndex);
            BlobsterSceneTransition.LoadScene(GameScene, transitionStyle, ActiveWorld?.Transition);
        }
    }
}
