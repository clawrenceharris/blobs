using System;
using System.Collections.Generic;
using Blobs.Core;
using UnityEngine;

namespace Blobs.Content
{
    public static class LevelAssetMapper
    {
        public static LevelDefinition ToCore(LevelDefinitionAsset asset)
        {
            if (asset == null)
                throw new ArgumentNullException(nameof(asset));


            var blobs = new List<BlobDefinition>(asset.Blobs.Count);
            foreach (BlobAssetData blob in asset.Blobs)
            {
                blobs.Add(new NormalBlobDefinition(
                    blob.id,
                    ToCore(blob.position),
                     blob.color,
                    blob.size));
            }

            var tiles = new List<TileDefinition>(asset.Tiles.Count);
            foreach (TileAssetData tile in asset.Tiles)
            {
                tiles.Add(ToCore(tile));
            }

            var definition = new LevelDefinition(
                asset.LevelId,
                asset.SchemaVersion,
                asset.Width,
                asset.Height,
                blobs,
                tiles
                );

            LevelValidator.ValidateOrThrow(definition);
            return definition;
        }

        private static GridPosition ToCore(Vector2Int position) =>
            new GridPosition(position.x, position.y);

        private static TileDefinition ToCore(TileAssetData tile)
        {
            if (tile == null)
                throw new InvalidOperationException("Level contains an empty tile entry.");

            GridPosition position = ToCore(tile.position);
            switch (tile)
            {
                case NormalTileAssetData normal:
                    return new NormalTileDefinition(normal.id, position);
                default:
                    throw new InvalidOperationException(
                        $"Unsupported tile asset data type: {tile.GetType().Name}.");
            }
        }
    }
}
