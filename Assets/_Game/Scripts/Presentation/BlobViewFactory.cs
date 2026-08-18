using Blobs.Application;
using Blobs.Content;
using Blobs.Core;
using UnityEngine;

namespace Blobs.Presentation
{
    public interface IBlobViewFactory
    {
        BlobView Create(
            BlobState blob,
            IGameplayState gameplayState,
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
            IGameplayState state,
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
                state,

                _catalog.GetRequiredColorPalette(),
                cellSize,
                origin);

            return instance;
        }
    }
}
