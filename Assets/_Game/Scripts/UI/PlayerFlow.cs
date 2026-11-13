using Blobs.Application;
using UnityEngine.SceneManagement;

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

        public static void GoHome()
        {
            PlayerFlowState.Clear();
            SceneManager.LoadScene(HomeScene);
        }

        public static void GoLevels()
        {
            PlayerFlowState.Clear();
            SceneManager.LoadScene(LevelsScene);
        }

        public static void GoGame(int levelIndex)
        {
            PlayerFlowState.SetPendingLevelIndex(levelIndex);
            SceneManager.LoadScene(GameScene);
        }
    }
}
