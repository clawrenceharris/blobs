using System;
using System.Linq;
using Blobs.Core;
using NUnit.Framework;

namespace Blobs.Tests.EditMode
{
    public sealed class TrailMechanicTests
    {
        [Test]
        public void TrailMoveSpawnsNormalTrailColorBlobsExceptOnMergeSites()
        {
            BoardState board = Board(
                TrailBlob("source", BlobColor.Red, BlobColor.Green, 0),
                NormalBlob("middle", BlobColor.Blue, 1),
                NormalBlob("target", BlobColor.Blue, 4));

            MoveResult result = Resolve(board);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Steps.Select(step => step.Kind), Is.EqualTo(new[]
            {
                MoveStepKind.Merge,
                MoveStepKind.Traverse,
                MoveStepKind.Traverse,
                MoveStepKind.Merge
            }));
            Assert.That(result.Effects, Is.EqualTo(result.Steps.SelectMany(step => step.Effects)));
            Assert.That(board.GetBlob("source").Position, Is.EqualTo(new GridPosition(4, 0)));
            Assert.That(board.GetBlob("source").Components.Color?.Color, Is.EqualTo(BlobColor.Red));
            Assert.That(board.GetBlobAt(new GridPosition(1, 0)), Is.Null);

            BlobState[] spawned = board.Blobs
                .Where(blob => blob.Id != "source")
                .OrderBy(blob => blob.Position.X)
                .ToArray();
            Assert.That(spawned.Select(blob => blob.Position.X), Is.EqualTo(new[] { 0, 2, 3 }));
            Assert.That(spawned.Select(blob => blob.Type), Is.All.EqualTo(BlobType.Normal));
            Assert.That(
                spawned.Select(blob => blob.Components.Color?.Color),
                Is.All.EqualTo(BlobColor.Green));
        }

        [Test]
        public void TrailMoveRejectsInvalidLaterMergeWithoutPartialSpawns()
        {
            BoardState board = Board(
                TrailBlob("source", BlobColor.Red, BlobColor.Green, 0),
                NormalBlob("target", BlobColor.Red, 3));
            string before = Snapshot(board);

            MoveResult result = Resolve(board);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.FailureReason,
                Is.EqualTo(MoveFailureReason.NormalMergeRequiresDifferentColors));
            Assert.That(result.Steps, Is.Empty);
            Assert.That(result.Effects, Is.Empty);
            Assert.That(Snapshot(board), Is.EqualTo(before));
        }

        private static MoveResult Resolve(BoardState board)
        {
            return new MoveResolver().Resolve(
                board,
                new MoveIntent(board.GetBlob("source"), board.GetBlob("target")));
        }

        private static BoardState Board(params BlobState[] blobs)
        {
            return new BoardState(5, 1, blobs, Array.Empty<TileState>());
        }

        private static BlobState NormalBlob(string id, BlobColor color, int x)
        {
            return new BlobState(id, BlobType.Normal, new GridPosition(x, 0))
                .WithColor(color);
        }

        private static BlobState TrailBlob(
            string id,
            BlobColor bodyColor,
            BlobColor trailColor,
            int x)
        {
            return new BlobState(id, BlobType.Trail, new GridPosition(x, 0))
                .WithColor(bodyColor)
                .WithTrail(trailColor);
        }

        private static string Snapshot(BoardState board)
        {
            return string.Join(
                ";",
                board.Blobs
                    .OrderBy(blob => blob.Id)
                    .Select(blob =>
                        $"{blob.Id}:{blob.Type}:{blob.Position}:{blob.Components.Color?.Color}:{blob.Components.Trail?.TrailColor}"));
        }
    }
}
