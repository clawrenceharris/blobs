using Blobs.Content;
using Blobs.Core;
using UnityEngine;

namespace Blobs.Presentation
{
    public interface IBlobViewFactory
    {
        BlobView Create(
            BlobState blob,
            Transform parent,
            float cellSize,
            Vector2 origin);
    }

    public sealed class BlobViewFactory : IBlobViewFactory
    {
        private readonly BlobViewCatalogAsset _catalog;
        private readonly LevelColorPaletteAsset _palette;
        public BlobViewFactory(BlobViewCatalogAsset catalog, LevelColorPaletteAsset palette)
        {
            _catalog = catalog != null
                ? catalog
                : throw new System.ArgumentNullException(nameof(catalog));
            _palette = palette != null
                ? palette
                : throw new System.ArgumentNullException(nameof(palette));
        }

        public BlobView Create(
            BlobState blob,
            Transform parent,
            float cellSize,
            Vector2 origin
            )
        {
            BlobView prefab =
                _catalog.GetRequiredPrefab(blob.Type);

            BlobView instance =
                Object.Instantiate(prefab, parent);

            instance.Initialize(
                blob,
                _palette,
                cellSize,
                origin);

            return instance;
        }
    }
}
