using System;
using System.Collections.Generic;
using Blobs.Core;
using UnityEngine;

namespace Blobs.Content
{
    [CreateAssetMenu(
        fileName = "World_New",
        menuName = "Blobs/World Definition")]
    public sealed class WorldDefinitionAsset : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string worldId = "world-new";
        [SerializeField] private string worldTitle = "New World";
        [SerializeField, TextArea(1, 3)] private string worldDescription;
        [SerializeField, Min(1)] private int schemaVersion = 1;

        [Header("Mechanics")]
        [SerializeField] private BlobType blobType;
        [SerializeField] private TileType tileType;
        [SerializeField] private MechanicType mechanicType;

        [Header("Levels")]
        [SerializeReference] private List<LevelDefinitionAsset> levels = new();
        [SerializeField, Min(1)] private int levelSlotCount = 10;

        [Header("Presentation")]
        [SerializeField] private LevelColorPaletteAsset palette;
        [SerializeField] private GameObject mechanicPreviewPrefab;
        [SerializeField] private GameObject mechanicPreviewLockedPrefab;
        [SerializeField] private bool unlocked;

        [Header("Transition")]
        [SerializeField] private WorldTransitionPresentation transition = new();

        public LevelColorPaletteAsset Palette => palette;
        public GameObject MechanicPreviewPrefab => mechanicPreviewPrefab;
        public GameObject MechanicPreviewLockedPrefab => mechanicPreviewLockedPrefab;
        public bool Unlocked => unlocked;
        public WorldTransitionPresentation Transition => transition;
        public int LevelSlotCount => levelSlotCount;
        public string WorldId => worldId;
        public string WorldTitle => worldTitle;
        public string WorldDescription => worldDescription;
        public int SchemaVersion => schemaVersion;
        public BlobType BlobType => blobType;
        public TileType TileType => tileType;
        public MechanicType MechanicType => mechanicType;
        public IReadOnlyList<LevelDefinitionAsset> Levels => levels;

    }


}
