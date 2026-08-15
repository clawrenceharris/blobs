using Blobs.Content;
using Blobs.Core;
using UnityEngine;

namespace Blobs.Presentation
{
    public interface IBlobViewFactory
    {
        BlobView Create(
            BlobState blob,
            LevelVisualThemeAsset theme,
            Transform parent,
            float cellSize,
            Vector2 origin);
    }

    public sealed class BlobViewFactory : IBlobViewFactory
    {
        private readonly BlobViewCatalogAsset _catalog;

        public BlobViewFactory(BlobViewCatalogAsset catalog)
        {
            _catalog = catalog != null
                ? catalog
                : throw new System.ArgumentNullException(nameof(catalog));
        }

        public BlobView Create(
            BlobState blob,
            LevelVisualThemeAsset theme,
            Transform parent,
            float cellSize,
            Vector2 origin)
        {
            BlobView prefab =
                _catalog.GetRequiredPrefab(blob.Type);

            BlobView instance =
                Object.Instantiate(prefab, parent);

            instance.Initialize(
                blob,
                theme,
                cellSize,
                origin);

            return instance;
        }
    }
}