using UnityEngine;
using System.Collections.Generic;
using Blobs.Blobs;

namespace Blobs.Core
{
    /// <summary>
    /// ScriptableObject containing level data.
    /// Create via Assets > Create > Blobs > Level Data
    /// </summary>
    [CreateAssetMenu(fileName = "Level_", menuName = "Blobs/Level Data", order = 1)]
    public class LevelData : ScriptableObject
    {
        [Header("Level Info")]
        public int levelNumber = 1;
        public string levelName = "New Level";
        
        [Header("Grid Size")]
        public int width = 5;
        public int height = 5;

        [Header("Scoring")]
        public int minMoves = 5;
        public int baseScore = 1000;
        public int movePenalty = 50;
        public int undoPenalty = 100;
        [Tooltip("Score thresholds for 1, 2, 3 stars")]
        public int[] starThresholds = new int[] { 300, 600, 900 };

        [Header("Tutorial")]
        public bool isTutorial = false;
        [TextArea(2, 5)]
        public List<string> tutorialMessages = new List<string>();

        [Header("Blobs")]
        public List<BlobSpawnData> blobs = new List<BlobSpawnData>();

        [Header("Tiles (Optional - empty = all normal tiles)")]
        public List<TileSpawnData> tiles = new List<TileSpawnData>();
    }

    [System.Serializable]
    public class BlobSpawnData
    {
        public Vector2Int position;
        public BlobType type = BlobType.Normal;
        public BlobColor color = BlobColor.Pink;
        
        [Tooltip("Optional: Size for certain blob types")]
        public int size = 1;
    }

    [System.Serializable]
    public class TileSpawnData
    {
        public Vector2Int position;
        public TileType type = TileType.Normal;
    }

    public enum TileType
    {
        Normal,     // Regular tile
        Blocked,    // No blob can be placed
        Goal,       // Special goal tile (visual only)
        Ice,        // Slippery - blob slides further
        Sticky      // Blob sticks here
    }
}
