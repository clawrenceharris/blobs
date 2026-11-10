using System;
using System.Collections.Generic;
using System.Linq;
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
            var emptyPositions = new List<GridPosition>(asset.EmptyPositions.Count);
            foreach (Vector2Int position in asset.EmptyPositions)
            {
                emptyPositions.Add(ToCore(position));
            }

            var objectiveItems = new List<LevelObjectiveItemDefinition>();
            if (asset.Objective?.items != null)
            {
                foreach (LevelObjectiveItemAssetData item in asset.Objective.items)
                    objectiveItems.Add(ToCore(item));
            }

            var definition = new LevelDefinition(
                asset.LevelId,
                asset.SchemaVersion,
                asset.Width,
                asset.Height,
                blobs,
                tiles,
                emptyPositions: emptyPositions,
                objective: new LevelObjectiveDefinition(objectiveItems));

            LevelValidator.ValidateOrThrow(definition);
            return definition;
        }

        /// <summary>
        /// Maps only the blobs of an asset, without level validation. Used for tutorial staging assets
        /// that hold pieces revealed later and are not playable boards themselves.
        /// </summary>
        public static IReadOnlyList<BlobDefinition> ToCoreBlobs(LevelDefinitionAsset asset)
        {
            if (asset == null)
                throw new ArgumentNullException(nameof(asset));

            var blobs = new List<BlobDefinition>(asset.Blobs.Count);
            foreach (BlobAssetData blob in asset.Blobs)
                blobs.Add(ToCore(blob));
            return blobs;
        }

        private static LevelObjectiveItemDefinition ToCore(LevelObjectiveItemAssetData item)
        {
            if (item == null)
                throw new InvalidOperationException("Level contains an empty objective entry.");

            return new LevelObjectiveItemDefinition(ToCore(item.target), item.amount);
        }

        private static ObjectiveTarget ToCore(ObjectiveTargetAssetData target)
        {
            switch (target)
            {
                case BlobObjectiveTargetAssetData blob:
                    return new BlobObjectiveTarget(
                        blob.type,
                        blob.matchColor ? blob.color : null,
                        blob.matchSize ? blob.size : null);
                case TileObjectiveTargetAssetData tile:
                    return new TileObjectiveTarget(tile.type);
                case null:
                    throw new InvalidOperationException("Level objective entry has no target.");
                default:
                    throw new InvalidOperationException(
                        $"Unsupported objective target asset data type: {target.GetType().Name}.");
            }
        }

        private static GridPosition ToCore(Vector2Int position) => new(position.x, position.y);

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
                        normal.color,
                        normal.size,
                        normal.facing);
                case NormalBlobAssetData normal:
                    throw new InvalidOperationException(
                        $"Normal blob '{normal.id}' declares unsupported type {normal.type}.");
                case NestBlobAssetData nest when nest.type == BlobType.Nest:
                    return new NestBlobDefinition(
                        nest.id,
                        ToCore(nest.position),
                        nest.color,
                        nest.facing);
                case NestBlobAssetData nest:
                    throw new InvalidOperationException(
                        $"Nest blob '{nest.id}' declares unsupported type {nest.type}.");
                case TrailBlobAssetData trail when trail.type == BlobType.Trail:
                    return new TrailBlobDefinition(
                        trail.id,
                        ToCore(trail.position),
                        trail.color,
                        trail.trailColor,
                        trail.size,
                        trail.facing);
                case TrailBlobAssetData trail:
                    throw new InvalidOperationException(
                        $"Trail blob '{trail.id}' declares unsupported type {trail.type}.");
                case RockBlobAssetData rock when rock.type == BlobType.Rock:
                    return new RockBlobDefinition(
                        rock.id,
                        ToCore(rock.position),
                        rock.facing);
                case RockBlobAssetData rock:
                    throw new InvalidOperationException(
                        $"Rock blob '{rock.id}' declares unsupported type {rock.type}.");

                case GhostBlobAssetData ghost when ghost.type == BlobType.Ghost:
                    return new GhostBlobDefinition(
                        ghost.id,
                        ToCore(ghost.position),
                        ghost.facing);
                case GhostBlobAssetData ghost:
                    throw new InvalidOperationException(
                        $"Ghost blob '{ghost.id}' declares unsupported type {ghost.type}.");


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

                case GraveTileAssetData grave when grave.type == TileType.Grave:
                    return new GraveTileDefinition(grave.id, position);
                case GraveTileAssetData grave:
                    throw new InvalidOperationException(
                        $"Grave tile '{grave.id}' declares unsupported type {grave.type}.");
                default:
                    throw new InvalidOperationException(
                        $"Unsupported tile asset data type: {tile.GetType().Name}.");
            }
        }
    }

}
