using System;
using System.Collections.Generic;
using Blobs.Core;
using UnityEngine;

namespace Blobs.UI
{
    public interface IObjectiveViewCatalogEntry<TType, TView>
    {
        TType Type { get; }
        TView Prefab { get; }
    }

    [Serializable]
    public sealed class ObjectiveViewCatalogEntry : IObjectiveViewCatalogEntry<BlobType, GameplayObjectiveView>
    {
        [SerializeField] private BlobType type;
        [SerializeField] private GameplayObjectiveView prefab;

        public BlobType Type => type;
        public GameplayObjectiveView Prefab => prefab;
    }

    [Serializable]
    public sealed class TileObjectiveViewCatalogEntry : IObjectiveViewCatalogEntry<TileType, GameplayObjectiveView>
    {
        [SerializeField] private TileType type;
        [SerializeField] private GameplayObjectiveView prefab;

        public TileType Type => type;
        public GameplayObjectiveView Prefab => prefab;
    }

    /// <summary>
    /// Maps objective targets to HUD counter prefabs. Prefabs are chosen by blob or tile type;
    /// the view tints itself for the target color.
    /// </summary>
    [CreateAssetMenu(
        fileName = "ObjectiveViewCatalog",
        menuName = "Blobs/Presentation/Objective View Catalog")]
    public sealed class ObjectiveViewCatalogAsset : ScriptableObject
    {
        [SerializeField] private List<ObjectiveViewCatalogEntry> objectiveEntries = new();
        [SerializeField] private List<TileObjectiveViewCatalogEntry> tileObjectiveEntries = new();

        private Dictionary<BlobType, GameplayObjectiveView> _blobLookup;
        private Dictionary<TileType, GameplayObjectiveView> _tileLookup;

        private void OnEnable()
        {
            ClearLookups();
        }

        public GameplayObjectiveView GetRequiredObjectivePrefab(ObjectiveTarget target)
        {
            switch (target)
            {
                case BlobObjectiveTarget blob:
                    _blobLookup ??= BuildLookup<BlobType, ObjectiveViewCatalogEntry>(objectiveEntries);
                    return GetRequired(_blobLookup, blob.Type);
                case TileObjectiveTarget tile:
                    _tileLookup ??= BuildLookup<TileType, TileObjectiveViewCatalogEntry>(tileObjectiveEntries);
                    return GetRequired(_tileLookup, tile.Type);
                default:
                    throw new InvalidOperationException(
                        $"Unsupported objective target: {target?.GetType().Name ?? "null"}.");
            }
        }

        private void ClearLookups()
        {
            _blobLookup = null;
            _tileLookup = null;
        }

        private static Dictionary<TType, GameplayObjectiveView> BuildLookup<TType, TEntry>(
            IEnumerable<TEntry> entries)
            where TEntry : IObjectiveViewCatalogEntry<TType, GameplayObjectiveView>
        {
            var lookup = new Dictionary<TType, GameplayObjectiveView>();
            foreach (TEntry entry in entries)
            {
                if (entry == null || entry.Prefab == null)
                    continue;

                if (!lookup.TryAdd(entry.Type, entry.Prefab))
                {
                    throw new InvalidOperationException(
                        $"Duplicate GameplayObjectiveView registration for {entry.Type}.");
                }
            }

            return lookup;
        }

        private static GameplayObjectiveView GetRequired<TType>(
            Dictionary<TType, GameplayObjectiveView> lookup,
            TType type)
        {
            if (!lookup.TryGetValue(type, out GameplayObjectiveView prefab) || prefab == null)
            {
                throw new InvalidOperationException(
                    $"No GameplayObjectiveView prefab is registered for {type}.");
            }

            return prefab;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ClearLookups();
            ReportInvalidEntries<BlobType, ObjectiveViewCatalogEntry>(objectiveEntries);
            ReportInvalidEntries<TileType, TileObjectiveViewCatalogEntry>(tileObjectiveEntries);
        }

        private void ReportInvalidEntries<TType, TEntry>(IEnumerable<TEntry> entries)
            where TEntry : IObjectiveViewCatalogEntry<TType, GameplayObjectiveView>
        {
            var registered = new HashSet<TType>();
            foreach (TEntry entry in entries)
            {
                if (entry == null)
                    continue;

                if (entry.Prefab == null)
                    Debug.LogError($"Objective view entry {entry.Type} has no prefab.", this);

                if (!registered.Add(entry.Type))
                    Debug.LogError($"Objective type {entry.Type} is registered more than once.", this);
            }
        }
#endif
    }
}
