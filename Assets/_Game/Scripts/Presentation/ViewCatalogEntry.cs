using System;
using Blobs.Core;
using UnityEngine;

namespace Blobs.Presentation
{
    public interface IViewCatalogEntry<TType, TView>
    {
        TType Type { get; }
        TView Prefab { get; }
    }
    [Serializable]
    public sealed class BlobViewCatalogEntry : IViewCatalogEntry<BlobType, BlobView>
    {
        [SerializeField] private BlobType type;
        [SerializeField] private BlobView prefab;

        public BlobType Type => type;
        public BlobView Prefab => prefab;
    }

    [Serializable]
    public sealed class TileViewCatalogEntry : IViewCatalogEntry<TileType, TileView>
    {
        [SerializeField] private TileType type;
        [SerializeField] private TileView prefab;

        public TileType Type => type;
        public TileView Prefab => prefab;
    }
}