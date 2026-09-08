using UnityEngine;

namespace Blobs.Core
{
    /// <summary>
    /// Simple save/load manager for level progress using PlayerPrefs.
    /// Tracks level completion and star ratings (0-3 stars per level).
    /// </summary>
    public static class LevelProgressManager
    {
        private const string STARS_KEY_PREFIX = "Level_Stars_";
        private const int MAX_STARS = 3;
        private const int TOTAL_LEVELS = 9;

        #region Star Management

        /// <summary>
        /// Get the star rating for a specific level.
        /// </summary>
        /// <param name="levelIndex">Level index (0-8)</param>
        /// <returns>Star count (0-3)</returns>
        public static int GetStars(int levelIndex)
        {
            return PlayerPrefs.GetInt(GetStarsKey(levelIndex), 0);
        }

        /// <summary>
        /// Set the star rating for a specific level.
        /// Only updates if new stars are higher than existing.
        /// </summary>
        /// <param name="levelIndex">Level index (0-8)</param>
        /// <param name="stars">Star count (0-3)</param>
        public static void SetStars(int levelIndex, int stars)
        {
            int clampedStars = Mathf.Clamp(stars, 0, MAX_STARS);
            int currentStars = GetStars(levelIndex);

            // Only save if new score is higher (best score system)
            if (clampedStars > currentStars)
            {
                PlayerPrefs.SetInt(GetStarsKey(levelIndex), clampedStars);
                PlayerPrefs.Save();
            }
        }

        #endregion

        #region Level Unlock

        /// <summary>
        /// Check if a level is unlocked.
        /// Level 0 is always unlocked.
        /// Other levels unlock when the previous level has at least 1 star.
        /// </summary>
        /// <param name="levelIndex">Level index (0-8)</param>
        public static bool IsLevelUnlocked(int levelIndex)
        {
            // Level 0 (first level) is always unlocked
            if (levelIndex <= 0)
                return true;

            // Other levels require previous level to be completed (1+ stars)
            return GetStars(levelIndex - 1) > 0;
        }

        /// <summary>
        /// Get the highest unlocked level index.
        /// </summary>
        public static int GetHighestUnlockedLevel()
        {
            for (int i = TOTAL_LEVELS - 1; i >= 0; i--)
            {
                if (IsLevelUnlocked(i))
                    return i;
            }
            return 0;
        }

        /// <summary>
        /// Check if a level has been completed (at least 1 star).
        /// </summary>
        public static bool IsLevelCompleted(int levelIndex)
        {
            return GetStars(levelIndex) > 0;
        }

        #endregion

        #region Utility

        /// <summary>
        /// Get total stars collected across all levels.
        /// </summary>
        public static int GetTotalStars()
        {
            int total = 0;
            for (int i = 0; i < TOTAL_LEVELS; i++)
            {
                total += GetStars(i);
            }
            return total;
        }

        /// <summary>
        /// Reset all progress (for testing or new game).
        /// </summary>
        public static void ResetAllProgress()
        {
            for (int i = 0; i < TOTAL_LEVELS; i++)
            {
                PlayerPrefs.DeleteKey(GetStarsKey(i));
            }
            PlayerPrefs.Save();
            Debug.Log("[LevelProgressManager] All progress reset.");
        }

        private static string GetStarsKey(int levelIndex)
        {
            return STARS_KEY_PREFIX + levelIndex;
        }

        #endregion
    }
}
