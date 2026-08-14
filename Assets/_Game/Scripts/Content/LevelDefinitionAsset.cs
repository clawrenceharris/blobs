using System;
using System.Collections.Generic;
using Blobs.Core;
using UnityEngine;

namespace Blobs.Content
{
    [CreateAssetMenu(
        fileName = "Level_New",
        menuName = "Blobs/Level Definition")]
    public sealed class LevelDefinitionAsset : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string levelId = "level-new";
        [SerializeField, Min(1)] private int schemaVersion = 1;

        [Header("Board")]
        [SerializeField, Min(1)] private int width = 6;
        [SerializeField, Min(1)] private int height = 6;
        [SerializeField, Min(1)] private int maximumBeats = 12;

        [Header("Initial State")]
        [SerializeReference] private List<BlobAssetData> blobs = new();
        [SerializeReference] private List<TileAssetData> tiles = new();

        [Header("Objective")]
        [SerializeField] private LevelObjectiveType objective =
            LevelObjectiveType.ClearAllClearableBlobs;

        [Header("Presentation")]
        [SerializeField] private LevelVisualThemeAsset visualTheme;

        public LevelVisualThemeAsset VisualTheme => visualTheme;
        public string LevelId => levelId;
        public int SchemaVersion => schemaVersion;
        public int Width => width;
        public int Height => height;
        public int MaximumBeats => maximumBeats;
        public IReadOnlyList<BlobAssetData> Blobs => blobs;
        public IReadOnlyList<TileAssetData> Tiles => tiles;
        public LevelObjectiveType Objective => objective;
    }


    [Serializable]
    public abstract class BlobAssetData
    {
        public string id;
        public Vector2Int position;
        public BlobColor color;
        public BlobType type;
        public BlobSize size;
    }

    [Serializable]
    public sealed class NormalBlobAssetData : BlobAssetData
    {
    }

   

    [Serializable]
    public abstract class TileAssetData
    {
        public string id;
        public Vector2Int position;
        public TileType type;
    }

    [Serializable]
    public sealed class NormalTileAssetData : TileAssetData
    {
    }

    [Serializable]
    public sealed class EmptyTileAssetData : TileAssetData
    {
    }
}
