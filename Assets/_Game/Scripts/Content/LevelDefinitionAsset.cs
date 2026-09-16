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
        [SerializeReference] private List<Vector2Int> emptyPositions = new();


        [Header("Initial State")]
        [SerializeReference] private List<BlobAssetData> blobs = new();
        [SerializeReference] private List<TileAssetData> tiles = new();


        [Header("Objective")]
        [SerializeField]
        private LevelObjectiveType objective = LevelObjectiveType.ClearAllClearableBlobs;

        [Header("Presentation")]
        [SerializeField] private LevelColorPaletteAsset palette;

        public LevelColorPaletteAsset Palette => palette;
        public string LevelId => levelId;
        public int SchemaVersion => schemaVersion;
        public int Width => width;
        public int Height => height;
        public IReadOnlyList<BlobAssetData> Blobs => blobs;

        public IReadOnlyList<TileAssetData> Tiles => tiles;
        public LevelObjectiveType Objective => objective;
        public IReadOnlyList<Vector2Int> EmptyPositions => emptyPositions;
    }


    [Serializable]
    public abstract class BlobAssetData
    {
        public string id;
        public Vector2Int position;
        public BlobType type;

    }

    [Serializable]
    public abstract class ColorBlobAssetData : BlobAssetData
    {
        public BlobColor color;
    }

    [Serializable]
    public sealed class NormalBlobAssetData : ColorBlobAssetData
    {
    }

    [Serializable]
    public sealed class FlagBlobAssetData : ColorBlobAssetData
    {

    }

    [Serializable]
    public sealed class TrailBlobAssetData : ColorBlobAssetData
    {
        [Tooltip("Color of the normal blobs left behind on tiles this blob departs.")]
        public BlobColor trailColor;

    }
    [Serializable]
    public sealed class RockBlobAssetData : BlobAssetData
    {
    }

    [Serializable]
    public sealed class GhostBlobAssetData : BlobAssetData
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
    public sealed class GraveTileAssetData : TileAssetData
    {
    }
}
