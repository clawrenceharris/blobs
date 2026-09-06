using System;
using Blobs.Core;
using UnityEngine;

namespace Blobs.Presentation
{
    /// <summary>
    /// Creates tile views without coupling <see cref="TilePresenter"/> to prefab selection.
    /// </summary>
    public interface ITileViewFactory
    {
        TileView Create(
            TileState tile,
            Transform parent,
            float cellSize,
            Vector2 origin);
    }

    public sealed class TileViewFactory : ITileViewFactory
    {
        private readonly TileViewCatalogAsset _catalog;

        public TileViewFactory(TileViewCatalogAsset catalog)
        {
            _catalog = catalog != null
                ? catalog
                : throw new ArgumentNullException(nameof(catalog));
        }

        public TileView Create(
            TileState tile,
            Transform parent,
            float cellSize,
            Vector2 origin)
        {
            TileView prefab = _catalog.GetRequiredPrefab(tile.Type);
            TileView instance = UnityEngine.Object.Instantiate(prefab, parent);
            instance.Initialize(tile, cellSize, origin);
            return instance;
        }
    }
}
