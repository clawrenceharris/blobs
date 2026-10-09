using System;
using System.Linq;
using Blobs.Content;
using Blobs.Core;
using NUnit.Framework;
using UnityEditor;

namespace Blobs.Tests.EditMode
{
    public sealed class BlobSizeMechanicTests
    {
        private const string SizeSlicePath =
            "Assets/_Game/Experiments/AbsorptionReadability/Levels/Level_Size_01_GrowThenYield.asset";

        [Test]
        public void AuthoredSizeSliceLoadsAndCanBeCompleted()
        {
            LevelDefinitionAsset asset =
                AssetDatabase.LoadAssetAtPath<LevelDefinitionAsset>(SizeSlicePath);
            Assert.That(asset, Is.Not.Null);

            LevelDefinition level = LevelAssetMapper.ToCore(asset);
            BoardState board = LevelFactory.CreateInitialBoard(level);
            Assert.That(board.GetBlob("small-source").Components.Size?.Size,
                Is.EqualTo(BlobSize.Small));
            Assert.That(board.GetBlob("small-meal").Components.Size?.Size,
                Is.EqualTo(BlobSize.Small));
            Assert.That(board.GetBlob("large-survivor").Components.Size?.Size,
                Is.EqualTo(BlobSize.Large));
            Assert.That(board.GetBlob("goal").HasSize, Is.False);

            MoveResult first = new MoveResolver().Resolve(
                board,
                new MoveIntent(
                    board.GetBlob("small-source"),
                    board.GetBlob("large-survivor")),
                level.Objective);
            Assert.That(first.Succeeded, Is.True);
            Assert.That(board.GetBlob("small-source"), Is.Null);
            Assert.That(board.GetBlob("small-meal"), Is.Null);
            Assert.That(board.GetBlob("large-survivor").Components.Size?.Size,
                Is.EqualTo(BlobSize.Large));

            MoveResult second = new MoveResolver().Resolve(
                board,
                new MoveIntent(
                    board.GetBlob("large-survivor"),
                    board.GetBlob("goal")),
                level.Objective);
            Assert.That(second.Succeeded, Is.True);
            Assert.That(second.IsComplete, Is.True);
        }

        [Test]
        public void SmallPlusSmallGrowsSurvivingMoverToNormal()
        {
            BoardState board = Board(
                Blob("source", BlobColor.Red, BlobSize.Small, 0),
                Blob("target", BlobColor.Blue, BlobSize.Small, 1));

            MoveResult result = Resolve(board);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(board.GetBlob("target"), Is.Null);
            Assert.That(board.GetBlob("source").Components.Size?.Size, Is.EqualTo(BlobSize.Normal));
            Assert.That(result.Effects.OfType<ChangeBlobSizeEffect>().Single().After,
                Is.EqualTo(BlobSize.Normal));
        }

        [Test]
        public void NormalPlusNormalRemainsNormal()
        {
            BoardState board = Board(
                Blob("source", BlobColor.Red, BlobSize.Normal, 0),
                Blob("target", BlobColor.Blue, BlobSize.Normal, 1));

            MoveResult result = Resolve(board);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(board.GetBlob("source").Components.Size?.Size, Is.EqualTo(BlobSize.Normal));
            Assert.That(result.Effects.OfType<ChangeBlobSizeEffect>(), Is.Empty);
        }

        [Test]
        public void LargerTargetConsumesSmallerMoverAndKeepsItsSize()
        {
            BoardState board = Board(
                Blob("source", BlobColor.Red, BlobSize.Small, 0),
                Blob("target", BlobColor.Blue, BlobSize.Large, 1));

            MoveResult result = Resolve(board);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(board.GetBlob("source"), Is.Null);
            Assert.That(board.GetBlob("target").Components.Size?.Size, Is.EqualTo(BlobSize.Large));
            Assert.That(result.Effects.OfType<ChangeBlobSizeEffect>(), Is.Empty);
        }

        [Test]
        public void LargerMoverConsumesSmallerTargetAndKeepsItsSize()
        {
            BoardState board = Board(
                Blob("source", BlobColor.Red, BlobSize.Large, 0),
                Blob("target", BlobColor.Blue, BlobSize.Small, 1));

            MoveResult result = Resolve(board);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(board.GetBlob("target"), Is.Null);
            Assert.That(board.GetBlob("source").Components.Size?.Size, Is.EqualTo(BlobSize.Large));
            Assert.That(result.Effects.OfType<ChangeBlobSizeEffect>(), Is.Empty);
        }

        private static BlobState Blob(
            string id,
            BlobColor color,
            BlobSize size,
            int x)
        {
            return new BlobState(id, BlobType.Normal, new GridPosition(x, 0))
                .WithColor(color)
                .WithSize(size);
        }

        private static BoardState Board(params BlobState[] blobs)
        {
            return new BoardState(2, 1, blobs, Array.Empty<TileState>());
        }

        private static MoveResult Resolve(BoardState board)
        {
            return new MoveResolver().Resolve(
                board,
                new MoveIntent(board.GetBlob("source"), board.GetBlob("target")));
        }
    }
}
