using System;
using System.Collections.Generic;
using Blobs.Core;
using UnityEngine;

namespace Blobs.Presentation
{
    [CreateAssetMenu(
        fileName = "ViewCatalog",
        menuName = "Blobs/Presentation/View Catalog")]
    public sealed class ViewCatalogAsset : ScriptableObject
    {
        [SerializeField] private List<TileViewCatalogEntry> tileEntries = new();
        [SerializeField] private List<BlobViewCatalogEntry> blobEntries = new();
        private Dictionary<TileType, TileView> _tileLookup;
        private Dictionary<BlobType, BlobView> _blobLookup;

        private void OnEnable()
        {
            _tileLookup = null;
            _blobLookup = null;
        }

        public TileView GetRequiredTilePrefab(TileType type)
        {
            EnsureTileLookup();

            if (!_tileLookup.TryGetValue(type, out TileView prefab) ||
                prefab == null)
            {
                throw new InvalidOperationException(
                     $"No TileView prefab is registered for {type}.");
            }

            return prefab;
        }

        public BlobView GetRequiredBlobPrefab(BlobType type)
        {
            EnsureBlobLookup();

            if (!_blobLookup.TryGetValue(type, out BlobView prefab) ||
                prefab == null)
            {
                throw new InvalidOperationException(
                    $"No BlobView prefab is registered for {type}.");
            }
            return prefab;
        }

        public void EnsureBlobLookup()
        {
            if (_blobLookup != null)
                return;

            _blobLookup = new Dictionary<BlobType, BlobView>();
            foreach (BlobViewCatalogEntry entry in blobEntries)
            {
                if (entry == null || entry.Prefab == null)
                    continue;

                if (!_blobLookup.TryAdd(entry.Type, entry.Prefab))
                {
                    throw new InvalidOperationException(
                        $"Duplicate BlobView registration for {entry.Type}.");
                }
            }
        }
        private void EnsureTileLookup()
        {
            if (_tileLookup != null)
                return;

            _tileLookup = new Dictionary<TileType, TileView>();

            foreach (TileViewCatalogEntry entry in tileEntries)
            {
                if (entry == null || entry.Prefab == null)
                    continue;

                if (!_tileLookup.TryAdd(entry.Type, entry.Prefab))
                {
                    throw new InvalidOperationException(
                        $"Duplicate TileView registration for {entry.Type}.");
                }
            }
        }



#if UNITY_EDITOR
        private void OnValidate()
        {
            _tileLookup = null;
            _blobLookup = null;

            var registeredTileTypes = new HashSet<TileType>();
            var registeredBlobTypes = new HashSet<BlobType>();

            foreach (IViewCatalogEntry<TileType, TileView> entry in tileEntries)
            {
                if (entry == null)
                    continue;

                if (entry.Prefab == null)
                {
                    Debug.LogError(
                        $"Tile view entry {entry.Type} has no prefab.",
                        this);
                }

                if (!registeredTileTypes.Add(entry.Type))
                {
                    Debug.LogError(
                        $"Tile type {entry.Type} is registered more than once.",
                        this);
                }
            }
            foreach (IViewCatalogEntry<BlobType, BlobView> entry in blobEntries)
            {
                if (entry == null)
                    continue;

                if (entry.Prefab == null)
                {
                    Debug.LogError(
                        $"Blob view entry {entry.Type} has no prefab.");
                }

                if (!registeredBlobTypes.Add(entry.Type))
                {
                    Debug.LogError(
                        $"Blob type {entry.Type} is registered more than once.",
                        this);
                }
            }
        }


#endif
    }
}
