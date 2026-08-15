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
                blobs.Add(ToCore(blob));
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
                tiles,
                new LevelObjectiveDefinition(asset.Objective)
                );

            LevelValidator.ValidateOrThrow(definition);
            return definition;
        }

        private static GridPosition ToCore(Vector2Int position) =>
            new GridPosition(position.x, position.y);

        private static BlobDefinition ToCore(BlobAssetData blob)
        {
            if (blob == null)
                throw new InvalidOperationException("Level contains an empty blob entry.");

            switch (blob)
            {
                case NormalBlobAssetData normal when normal.type == BlobType.Normal:
                    return new NormalBlobDefinition(
                        normal.id,
                        ToCore(normal.position),
                        normal.color);
                case NormalBlobAssetData normal:
                    throw new InvalidOperationException(
                        $"Normal blob '{normal.id}' declares unsupported type {normal.type}.");
                case FlagBlobAssetData flag when flag.type == BlobType.Flag:
                    return new FlagBlobDefinition(
                        flag.id,
                        ToCore(flag.position),
                        flag.color);
                case FlagBlobAssetData flag:
                    throw new InvalidOperationException(
                        $"Flag blob '{flag.id}' declares unsupported type {flag.type}.");
                case TrailBlobAssetData trail when trail.type == BlobType.Trail:
                    return new TrailBlobDefinition(
                        trail.id,
                        ToCore(trail.position),
                        trail.color,
                        trail.trailColor);
                case TrailBlobAssetData trail:
                    throw new InvalidOperationException(
                        $"Trail blob '{trail.id}' declares unsupported type {trail.type}.");
                default:
                    throw new InvalidOperationException(
                        $"Unsupported blob asset data type: {blob.GetType().Name}.");
            }
        }

        private static TileDefinition ToCore(TileAssetData tile)
        {
            if (tile == null)
                throw new InvalidOperationException("Level contains an empty tile entry.");

            GridPosition position = ToCore(tile.position);
            switch (tile)
            {
                case NormalTileAssetData normal when normal.type == TileType.Normal:
                    return new NormalTileDefinition(normal.id, position);
                case NormalTileAssetData normal:
                    throw new InvalidOperationException(
                        $"Normal tile '{normal.id}' declares unsupported type {normal.type}.");
                default:
                    throw new InvalidOperationException(
                        $"Unsupported tile asset data type: {tile.GetType().Name}.");
            }
        }
    }
}
