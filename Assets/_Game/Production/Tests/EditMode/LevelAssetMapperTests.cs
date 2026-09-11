using System;
using System.Collections.Generic;
using Blobs.Core;
using Blobs.Content;
using NUnit.Framework;
using UnityEditor;

namespace Blobs.Tests.EditMode
{
    public sealed class LevelAssetMapperTests
    {
        private static readonly string[] AuthoredLevelPaths =
        {
            "Assets/_Game/Production/Content/Levels/SO/Level_01.asset",
            "Assets/_Game/Production/Content/Levels/SO/Level_02.asset",
            "Assets/_Game/Production/Content/Levels/SO/Level_03.asset"
        };

        [Test]
        public void AuthoredLevelsLoadAndMapToValidatedCoreDefinitions()
        {
            var levelIds = new HashSet<string>(StringComparer.Ordinal);

            foreach (string path in AuthoredLevelPaths)
            {
                var asset = AssetDatabase.LoadAssetAtPath<LevelDefinitionAsset>(path);
                Assert.That(asset, Is.Not.Null, $"Missing authored level at {path}.");

                LevelDefinition level = LevelAssetMapper.ToCore(asset);
                Assert.That(levelIds.Add(level.Id), Is.True, $"Duplicate level ID: {level.Id}.");
                Assert.That(level.SchemaVersion, Is.EqualTo(LevelDefinition.CurrentSchemaVersion));
                Assert.That(level.Blobs.Count, Is.GreaterThanOrEqualTo(2));
                Assert.That(level.Objective.Type,
                    Is.EqualTo(LevelObjectiveType.ClearAllClearableBlobs));
                Assert.DoesNotThrow(() => LevelFactory.CreateInitialBoard(level));
            }
        }

    }
}
