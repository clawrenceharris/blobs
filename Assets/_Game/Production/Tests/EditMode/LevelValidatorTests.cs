using System;
using System.Collections.Generic;
using Blobs.Core;
using NUnit.Framework;

namespace Blobs.Tests.EditMode
{
    public sealed class LevelValidatorTests
    {
        [Test]
        public void ValidatorRejectsDuplicateBlobIds()
        {
            LevelDefinition level = Level(blobs: new[]
            {
                Blob("duplicate", 0, 0),
                Blob("duplicate", 1, 0)
            });

            InvalidOperationException error = Assert.Throws<InvalidOperationException>(
                () => LevelValidator.ValidateOrThrow(level));

            Assert.That(error.Message, Does.Contain("Duplicate blob ID"));
        }

        [Test]
        public void ValidatorRejectsMultipleBlobsInOneCell()
        {
            LevelDefinition level = Level(blobs: new[]
            {
                Blob("red", 1, 1),
                Blob("blue", 1, 1, BlobColor.Blue)
            });

            InvalidOperationException error = Assert.Throws<InvalidOperationException>(
                () => LevelValidator.ValidateOrThrow(level));

            Assert.That(error.Message, Does.Contain("Multiple blobs occupy"));
        }

        [Test]
        public void ValidatorRejectsMultipleTilesInOneCell()
        {
            LevelDefinition level = Level(tiles: new[]
            {
                Tile("tile-a", 1, 1),
                Tile("tile-b", 1, 1)
            });

            InvalidOperationException error = Assert.Throws<InvalidOperationException>(
                () => LevelValidator.ValidateOrThrow(level));

            Assert.That(error.Message, Does.Contain("Multiple tiles occupy"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void ValidatorRejectsMissingBlobIds(string id)
        {
            LevelDefinition level = Level(blobs: new[] { Blob(id, 0, 0) });

            Assert.Throws<InvalidOperationException>(
                () => LevelValidator.ValidateOrThrow(level));
        }

        [Test]
        public void ValidatorRejectsOutOfBoundsCoordinates()
        {
            LevelDefinition level = Level(blobs: new[] { Blob("outside", 3, 0) });

            InvalidOperationException error = Assert.Throws<InvalidOperationException>(
                () => LevelValidator.ValidateOrThrow(level));

            Assert.That(error.Message, Does.Contain("out of bounds"));
        }

        [Test]
        public void ValidatorRejectsUnsupportedColorValues()
        {
            LevelDefinition level = Level(blobs: new[]
            {
                Blob("bad-color", 0, 0, (BlobColor)999)
            });

            InvalidOperationException error = Assert.Throws<InvalidOperationException>(
                () => LevelValidator.ValidateOrThrow(level));

            Assert.That(error.Message, Does.Contain("blob color"));
        }

        [Test]
        public void ValidatorRejectsUnsupportedObjectiveValues()
        {
            LevelDefinition level = Level(
                objective: new LevelObjectiveDefinition((LevelObjectiveType)999));

            InvalidOperationException error = Assert.Throws<InvalidOperationException>(
                () => LevelValidator.ValidateOrThrow(level));

            Assert.That(error.Message, Does.Contain("level objective"));
        }

        [Test]
        public void ValidatorRejectsUnsupportedSchemaVersions()
        {
            var level = new LevelDefinition(
                "future-schema",
                LevelDefinition.CurrentSchemaVersion + 1,
                3,
                3,
                Array.Empty<BlobDefinition>(),
                Array.Empty<TileDefinition>());

            InvalidOperationException error = Assert.Throws<InvalidOperationException>(
                () => LevelValidator.ValidateOrThrow(level));

            Assert.That(error.Message, Does.Contain("Unsupported schema version"));
        }

        private static LevelDefinition Level(
            IReadOnlyList<BlobDefinition> blobs = null,
            IReadOnlyList<TileDefinition> tiles = null,
            LevelObjectiveDefinition objective = null)
        {
            return new LevelDefinition(
                "validation-test",
                LevelDefinition.CurrentSchemaVersion,
                3,
                3,
                blobs ?? Array.Empty<BlobDefinition>(),
                tiles ?? Array.Empty<TileDefinition>(),
                objective: objective);
        }

        private static NormalBlobDefinition Blob(
            string id,
            int x,
            int y,
            BlobColor color = BlobColor.Red)
        {
            return new NormalBlobDefinition(
                id,
                new GridPosition(x, y),
                color);
        }

        private static GraveTileDefinition Tile(string id, int x, int y)
        {
            return new GraveTileDefinition(id, new GridPosition(x, y));
        }
    }
}
