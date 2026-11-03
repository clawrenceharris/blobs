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

        [SerializeField, Min(0), Tooltip("Soft efficiency target shown as current/par in the HUD. Zero hides the denominator.")]
        private int parMoves;

        [Header("Presentation")]
        [SerializeField] private LevelColorPaletteAsset palette;

        [Header("Coach")]
        [SerializeField, Tooltip("When enabled, plays a looping ghost swipe from coachFrom to coachTo until the player starts a drag.")]
        private bool showSwipeCoach;
        [SerializeField] private Vector2Int coachFrom;
        [SerializeField] private Vector2Int coachTo;
        [SerializeField, TextArea(1, 3), Tooltip("Optional. Leave empty to teach with the pointer alone.")]
        private string coachCaption;

        public LevelColorPaletteAsset Palette => palette;
        public bool ShowSwipeCoach => showSwipeCoach;
        public Vector2Int CoachFrom => coachFrom;
        public Vector2Int CoachTo => coachTo;
        public string CoachCaption => coachCaption;
        public string LevelId => levelId;
        public int SchemaVersion => schemaVersion;
        public int Width => width;
        public int Height => height;
        public IReadOnlyList<BlobAssetData> Blobs => blobs;

        public IReadOnlyList<TileAssetData> Tiles => tiles;
        public LevelObjectiveType Objective => objective;
        public int ParMoves => parMoves;
        public IReadOnlyList<Vector2Int> EmptyPositions => emptyPositions;
    }


    [Serializable]
    public abstract class BlobAssetData
    {
        public string id;
        public Vector2Int position;
        public BlobType type;
        [Tooltip("Resting look direction. South faces the camera; East/West/North aim the face that way.")]
        public Direction facing = Direction.South;

    }



    [Serializable]
    public sealed class NormalBlobAssetData : BlobAssetData
    {
        public BlobColor color;
        public BlobSize size = BlobSize.Normal;

    }

    [Serializable]
    public sealed class FlagBlobAssetData : BlobAssetData
    {
        public BlobColor color;

    }

    [Serializable]
    public sealed class TrailBlobAssetData : BlobAssetData
    {
        [Tooltip("Color of the normal blobs left behind on tiles this blob departs.")]
        public BlobColor trailColor;
        public BlobColor color;
        public BlobSize size = BlobSize.Normal;


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
