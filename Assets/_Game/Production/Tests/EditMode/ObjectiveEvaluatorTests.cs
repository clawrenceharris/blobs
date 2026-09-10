using System;
using Blobs.Core;
using NUnit.Framework;

namespace Blobs.Tests.EditMode
{
    public sealed class ObjectiveEvaluatorTests
    {
        [TestCase(BlobType.Normal, false)]
        [TestCase(BlobType.Trail, false)]
        [TestCase(BlobType.Ghost, false)]
        [TestCase(BlobType.Rock, true)]
        public void CompletionDependsOnRemainingClearableBlobs(BlobType type, bool expected)
        {
            var board = new BoardState(2, 1, new[]
            {
                new BlobState("flag", BlobType.Flag, new GridPosition(0, 0)).WithColor(BlobColor.Red),
                new BlobState("remaining", type, new GridPosition(1, 0)).WithColor(BlobColor.Red)
            }, Array.Empty<TileState>());
            Assert.That(ObjectiveEvaluator.IsComplete(board), Is.EqualTo(expected));
        }

        [Test]
        public void FlagAloneIsComplete()
        {
            var board = new BoardState(1, 1, new[]
            {
                new BlobState("flag", BlobType.Flag, new GridPosition(0, 0)).WithColor(BlobColor.Red)
            }, Array.Empty<TileState>());
            Assert.That(ObjectiveEvaluator.IsComplete(board), Is.True);
        }
    }
}
