using System;
using System.Collections.Generic;
using Blobs.Core;
using UnityEngine;

namespace Blobs.Presentation
{
    /// <summary>
    /// Maps logical tile types to their presentation prefabs. Adding a tile type only requires
    /// registering its prefab here rather than extending presenter conditionals.
    /// </summary>
    [CreateAssetMenu(
        fileName = "TileViewCatalog",
        menuName = "Blobs/Presentation/Tile View Catalog")]
    public sealed class TileViewCatalogAsset : ScriptableObject
    {
        [Serializable]
        private sealed class Entry
        {
            [SerializeField] private TileType type;
            [SerializeField] private TileView prefab;

            public TileType Type => type;
            public TileView Prefab => prefab;
        }

        [SerializeField] private List<Entry> entries = new();

        private Dictionary<TileType, TileView> _lookup;

        public TileView GetRequiredPrefab(TileType type)
        {
            EnsureLookup();

            if (!_lookup.TryGetValue(type, out TileView prefab) || prefab == null)
            {
                throw new InvalidOperationException(
                    $"No TileView prefab is registered for {type}.");
            }

            return prefab;
        }

        private void EnsureLookup()
        {
            if (_lookup != null)
                return;

            _lookup = new Dictionary<TileType, TileView>();
            foreach (Entry entry in entries)
            {
                if (entry == null || entry.Prefab == null)
                    continue;

                if (!_lookup.TryAdd(entry.Type, entry.Prefab))
                {
                    throw new InvalidOperationException(
                        $"Duplicate TileView registration for {entry.Type}.");
                }
            }
        }

        private void OnEnable()
        {
            _lookup = null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _lookup = null;
            var registeredTypes = new HashSet<TileType>();

            foreach (Entry entry in entries)
            {
                if (entry == null)
                    continue;

                if (entry.Prefab == null)
                {
                    Debug.LogError($"Tile view entry {entry.Type} has no prefab.", this);
                }
                else if (entry.Prefab.GetComponent<Collider2D>() == null)
                {
                    Debug.LogError(
                        $"Tile view prefab for {entry.Type} requires an authored Collider2D.",
                        entry.Prefab);
                }

                if (!registeredTypes.Add(entry.Type))
                {
                    Debug.LogError(
                        $"Tile type {entry.Type} is registered more than once.",
                        this);
                }
            }
        }
#endif
    }
}
