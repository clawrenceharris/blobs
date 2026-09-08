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
            var emptyPositions = new List<GridPosition>(asset.EmptyPositions.Count);
            foreach (Vector2Int position in asset.EmptyPositions)
            {
                emptyPositions.Add(ToCore(position));
            }

            var definition = new LevelDefinition(
                asset.LevelId,
                asset.SchemaVersion,
                asset.Width,
                asset.Height,
                blobs,
                tiles,
                emptyPositions: emptyPositions,
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
                case RockBlobAssetData rock when rock.type == BlobType.Rock:
                    return new RockBlobDefinition(
                        rock.id,
                        ToCore(rock.position));
                case RockBlobAssetData rock:
                    throw new InvalidOperationException(
                        $"Rock blob '{rock.id}' declares unsupported type {rock.type}.");

                case GhostBlobAssetData ghost when ghost.type == BlobType.Ghost:
                    return new GhostBlobDefinition(
                        ghost.id,
                        ToCore(ghost.position));
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

                case SigilTileAssetData sigil when sigil.type == TileType.Sigil:
                    return new SigilTileDefinition(sigil.id, position);
                case SigilTileAssetData sigil:
                    throw new InvalidOperationException(
                        $"Sigil tile '{sigil.id}' declares unsupported type {sigil.type}.");
                default:
                    throw new InvalidOperationException(
                        $"Unsupported tile asset data type: {tile.GetType().Name}.");
            }
        }
    }

}
