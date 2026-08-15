using System;
using System.Collections.Generic;
using Blobs.Content;
using Blobs.Core;
using UnityEngine;

namespace Blobs.Presentation
{
    [CreateAssetMenu(
        fileName = "BlobViewCatalog",
        menuName = "Blobs/Presentation/Blob View Catalog")]
    public sealed class BlobViewCatalogAsset : ScriptableObject
    {
        [Serializable]
        private sealed class Entry
        {
            [SerializeField] private BlobType type;
            [SerializeField] private BlobView prefab;

            public BlobType Type => type;
            public BlobView Prefab => prefab;
        }

        [SerializeField] private List<Entry> entries = new();
        [SerializeField] private BlobColorPaletteAsset colorPalette;

        private Dictionary<BlobType, BlobView> _lookup;

        public BlobColorPaletteAsset GetRequiredColorPalette()
        {
            return colorPalette != null
                ? colorPalette
                : throw new InvalidOperationException(
                    $"Blob view catalog '{name}' has no color palette assigned.");
        }

        public BlobView GetRequiredPrefab(BlobType type)
        {
            EnsureLookup();

            if (!_lookup.TryGetValue(type, out BlobView prefab) ||
                prefab == null)
            {
                throw new InvalidOperationException(
                    $"No BlobView prefab is registered for {type}.");
            }

            return prefab;
        }

        private void EnsureLookup()
        {
            if (_lookup != null)
                return;

            _lookup = new Dictionary<BlobType, BlobView>();

            foreach (Entry entry in entries)
            {
                if (entry == null || entry.Prefab == null)
                    continue;

                if (!_lookup.TryAdd(entry.Type, entry.Prefab))
                {
                    throw new InvalidOperationException(
                        $"Duplicate BlobView registration for {entry.Type}.");
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

            if (colorPalette == null)
                Debug.LogError("Blob view catalog has no color palette assigned.", this);

            var registeredTypes = new HashSet<BlobType>();

            foreach (Entry entry in entries)
            {
                if (entry == null)
                    continue;

                if (entry.Prefab == null)
                {
                    Debug.LogError(
                        $"Blob view entry {entry.Type} has no prefab.",
                        this);
                }

                if (!registeredTypes.Add(entry.Type))
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
