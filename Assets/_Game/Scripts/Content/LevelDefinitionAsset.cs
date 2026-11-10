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
        [SerializeField, Min(1)] private int width;
        [SerializeField, Min(1)] private int height;
        [SerializeReference] private List<Vector2Int> emptyPositions = new();


        [Header("Initial State")]
        [SerializeReference] private List<BlobAssetData> blobs = new();
        [SerializeReference] private List<TileAssetData> tiles = new();


        [SerializeField, Tooltip("Counters shown in the HUD. Leave empty to require clearing every clearable blob.")]
        private LevelObjectiveAssetData objective = new();

        [Header("Presentation")]
        [SerializeField] private LevelColorPaletteAsset palette;

        [Header("Coach")]
        [SerializeField, Tooltip("When enabled, plays a looping ghost swipe from coachFrom to coachTo until the player starts a drag.")]
        private bool showSwipeCoach;

        [SerializeField, Tooltip("When enabled, shows a pointer from coachFrom to coachTo.")]
        private bool showPointer;
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
        public LevelObjectiveAssetData Objective => objective;
        public IReadOnlyList<Vector2Int> EmptyPositions => emptyPositions;
        public bool ShowPointer => showPointer;
    }


    [Serializable]
    public sealed class LevelObjectiveAssetData
    {
        public List<LevelObjectiveItemAssetData> items = new();
    }

    [Serializable]
    public sealed class LevelObjectiveItemAssetData
    {
        [SerializeReference] public ObjectiveTargetAssetData target;
        [Min(1)] public int amount = 1;
    }

    [Serializable]
    public abstract class ObjectiveTargetAssetData
    {
    }

    [Serializable]
    public sealed class BlobObjectiveTargetAssetData : ObjectiveTargetAssetData
    {
        [Tooltip("Nests count when a monster is nested in them; other types count when cleared.")]
        public BlobType type;
        public bool matchColor = true;
        public BlobColor color;
        public bool matchSize;
        public BlobSize size = BlobSize.Normal;
    }

    [Serializable]
    public sealed class TileObjectiveTargetAssetData : ObjectiveTargetAssetData
    {
        [Tooltip("Graves count each time a ghost rests on one.")]
        public TileType type;
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
    public sealed class NestBlobAssetData : BlobAssetData
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
