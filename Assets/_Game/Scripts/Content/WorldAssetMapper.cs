using System;
using System.Collections.Generic;
using System.Linq;
using Blobs.Core;
using UnityEngine;

namespace Blobs.Content
{
    public static class WorldAssetMapper
    {
        public static WorldDefinition ToCore(WorldDefinitionAsset asset)
        {
            if (asset == null)
                throw new ArgumentNullException(nameof(asset));


            var levels = ToCoreLevels(asset);


            return new WorldDefinition(
                asset.WorldId,
                asset.WorldTitle,
                asset.WorldDescription,
                asset.SchemaVersion,
                levels.ToArray(),
                asset.BlobType,
                asset.TileType,
                asset.MechanicType);
        }



        public static IReadOnlyList<LevelDefinition> ToCoreLevels(WorldDefinitionAsset asset)
        {
            if (asset == null)
                throw new ArgumentNullException(nameof(asset));

            var levels = new List<LevelDefinition>(asset.Levels.Count);
            foreach (LevelDefinitionAsset level in asset.Levels)
                levels.Add(LevelAssetMapper.ToCore(level));
            return levels;
        }


    }

}
