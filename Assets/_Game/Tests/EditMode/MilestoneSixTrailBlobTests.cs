using System.Collections.Generic;
using System.Linq;
using Blobs.Application;
using Blobs.Core;
using NUnit.Framework;

namespace Blobs.Tests.EditMode
{
    public sealed class MilestoneSixTrailBlobTests
    {
        [Test]
        public void TrailMoveSpawnsTrailColorBlobsOnDepartedTilesButNotTarget()
        {
            GameSession session = CreateSession(
                "trail_straight_path",
                4,
                1,
                TrailBlob("trail_red", BlobColor.Red, BlobColor.Blue, 0, 0),
                NormalBlob("green_target", BlobColor.Green, 3, 0));

            MoveResult result = session.ExecuteMove(
                new MoveIntent("trail_red", "green_target"));
            GameSessionSnapshot snapshot = session.CreateSnapshot();

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Steps.Count, Is.EqualTo(3));
            Assert.That(result.Steps[0].Kind, Is.EqualTo(MoveStepKind.Traverse));
            Assert.That(result.Steps[1].Kind, Is.EqualTo(MoveStepKind.Traverse));
            Assert.That(result.Steps[2].Kind, Is.EqualTo(MoveStepKind.Merge));

            List<SpawnBlobEffect> spawns =
                result.Effects.OfType<SpawnBlobEffect>().ToList();

            Assert.That(spawns.Count, Is.EqualTo(3));
            Assert.That(
                spawns.Select(spawn => spawn.At),
                Is.EqualTo(new[]
                {
                    new GridPosition(0, 0),
                    new GridPosition(1, 0),
                    new GridPosition(2, 0)
                }));
            Assert.That(
                spawns.All(spawn => spawn.Blob.Type == BlobType.Normal),
                Is.True);
            Assert.That(
                spawns.All(spawn => spawn.Blob.Color == BlobColor.Blue),
                Is.True);

            // Trail blob survives on the target tile; three trail blobs remain behind it.
            Assert.That(snapshot.Blobs.Count, Is.EqualTo(4));
            BlobState mover = snapshot.Blobs.Single(blob => blob.Id == "trail_red");
            Assert.That(mover.Position, Is.EqualTo(new GridPosition(3, 0)));
            Assert.That(mover.Type, Is.EqualTo(BlobType.Trail));
            Assert.That(mover.TrailColor, Is.EqualTo(BlobColor.Blue));
        }

        [Test]
        public void SpawnedTrailBlobIdsAreDeterministicAndUnique()
        {
            GameSession session = CreateSession(
                "trail_spawn_ids",
                4,
                1,
                TrailBlob("trail_red", BlobColor.Red, BlobColor.Blue, 0, 0),
                NormalBlob("green_target", BlobColor.Green, 3, 0));

            MoveResult result = session.ExecuteMove(
                new MoveIntent("trail_red", "green_target"));

            List<string> spawnedIds = result.Effects
                .OfType<SpawnBlobEffect>()
                .Select(spawn => spawn.BlobId)
                .ToList();

            Assert.That(spawnedIds, Is.EqualTo(new[]
            {
                "trail_red-trail-1",
                "trail_red-trail-2",
                "trail_red-trail-3"
            }));
            Assert.That(spawnedIds.Distinct().Count(), Is.EqualTo(3));
        }

        [Test]
        public void IntermediateMergeSiteReceivesNoTrailSpawn()
        {
            // Your example: trail at 0,0, occupant at 1,0, target at 3,0. The
            // merge site (1,0) receives no trail blob; the origin (0,0) and the
            // traversed tile (2,0) do.
            GameSession session = CreateSession(
                "trail_chain_merge",
                4,
                1,
                TrailBlob("trail_red", BlobColor.Red, BlobColor.Blue, 0, 0),
                NormalBlob("purple_between", BlobColor.Purple, 1, 0),
                NormalBlob("green_target", BlobColor.Green, 3, 0));

            MoveResult result = session.ExecuteMove(
                new MoveIntent("trail_red", "green_target"));
            GameSessionSnapshot snapshot = session.CreateSnapshot();

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Steps.Count, Is.EqualTo(3));
            Assert.That(result.Steps[0].Kind, Is.EqualTo(MoveStepKind.Merge));
            Assert.That(result.Steps[1].Kind, Is.EqualTo(MoveStepKind.Traverse));
            Assert.That(result.Steps[2].Kind, Is.EqualTo(MoveStepKind.Merge));

            List<SpawnBlobEffect> spawns =
                result.Effects.OfType<SpawnBlobEffect>().ToList();

            Assert.That(
                spawns.Select(spawn => spawn.At),
                Is.EqualTo(new[]
                {
                    new GridPosition(0, 0),
                    new GridPosition(2, 0)
                }));

            // Trail blob at 3,0 plus two trail spawns; both occupants consumed.
            Assert.That(snapshot.Blobs.Count, Is.EqualTo(3));
            Assert.That(
                snapshot.Blobs.Any(blob => blob.Id == "purple_between"),
                Is.False);
            Assert.That(
                snapshot.Blobs.Single(blob => blob.Id == "trail_red").Position,
                Is.EqualTo(new GridPosition(3, 0)));
        }

        [Test]
        public void InvalidIntermediateMergeFailsAtomicallyWithoutSpawns()
        {
            // The occupant matches the trail blob's body color, so the chain link
            // is invalid and no effect (including trail spawns) touches the board.
            GameSession session = CreateSession(
                "trail_invalid_chain",
                4,
                1,
                TrailBlob("trail_red", BlobColor.Red, BlobColor.Blue, 0, 0),
                NormalBlob("red_between", BlobColor.Red, 1, 0),
                NormalBlob("green_target", BlobColor.Green, 3, 0));

            MoveResult result = session.ExecuteMove(
                new MoveIntent("trail_red", "green_target"));
            GameSessionSnapshot snapshot = session.CreateSnapshot();

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.FailureReason,
                Is.EqualTo(MoveFailureReason.NormalMergeRequiresDifferentColors));
            Assert.That(result.Effects, Is.Empty);
            Assert.That(snapshot.Blobs.Count, Is.EqualTo(3));
            Assert.That(
                snapshot.Blobs.Single(blob => blob.Id == "trail_red").Position,
                Is.EqualTo(new GridPosition(0, 0)));
            Assert.That(snapshot.MoveCount, Is.Zero);
        }

        [Test]
        public void NormalBlobsChainMergeThroughOccupants()
        {
            GameSession session = CreateSession(
                "normal_chain_merge",
                3,
                1,
                NormalBlob("red_source", BlobColor.Red, 0, 0),
                NormalBlob("blue_between", BlobColor.Blue, 1, 0),
                NormalBlob("green_target", BlobColor.Green, 2, 0));

            MoveResult result = session.ExecuteMove(
                new MoveIntent("red_source", "green_target"));
            GameSessionSnapshot snapshot = session.CreateSnapshot();

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Steps.Count, Is.EqualTo(2));
            Assert.That(
                result.Steps.All(step => step.Kind == MoveStepKind.Merge),
                Is.True);
            Assert.That(snapshot.Blobs.Count, Is.EqualTo(1));
            Assert.That(
                snapshot.Blobs.Single().Position,
                Is.EqualTo(new GridPosition(2, 0)));
            // The surviving mover is still clearable, so the objective is open.
            Assert.That(snapshot.IsComplete, Is.False);
        }

        [Test]
        public void ConsumingMergeMidPathEndsLocomotionBeforeIntentTarget()
        {
            // A strategy that consumes the mover mid-path stops the move even
            // though the intent named a farther blob.
            var rules = new BlobRuleBook(
                new Dictionary<BlobType, BlobTraits>
                {
                    [BlobType.Normal] =
                        new BlobTraits(canBeSource: true, isClearable: true)
                },
                new Dictionary<MergeKey, IMergeStrategy>
                {
                    [new MergeKey(BlobType.Normal, BlobType.Normal)] =
                        new ConsumingMergeStrategy()
                });
            var resolver = new MoveResolver(rules);
            var board = new BoardState(
                3,
                1,
                new[]
                {
                    new BlobState("mover", BlobType.Normal, BlobColor.Red, new GridPosition(0, 0)),
                    new BlobState("eater", BlobType.Normal, BlobColor.Blue, new GridPosition(1, 0)),
                    new BlobState("target", BlobType.Normal, BlobColor.Green, new GridPosition(2, 0))
                },
                new TileState[0]);

            MoveResult result = resolver.Resolve(
                board,
                new MoveIntent("mover", "target"));

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Steps.Count, Is.EqualTo(1));
            Assert.That(result.Steps[0].Kind, Is.EqualTo(MoveStepKind.Merge));
            Assert.That(board.GetBlob("mover"), Is.Null);
            Assert.That(board.GetBlob("eater"), Is.Null);
            Assert.That(
                board.GetBlob("target").Position,
                Is.EqualTo(new GridPosition(2, 0)));
        }

        [Test]
        public void TrailBlobCountsTowardClearObjective()
        {
            GameSession session = CreateSession(
                "trail_clearable",
                2,
                1,
                NormalBlob("red_source", BlobColor.Red, 0, 0),
                TrailBlob("trail_blue", BlobColor.Blue, BlobColor.Green, 1, 0));

            // Merging the normal blob into the adjacent trail blob leaves one
            // blob, so the clear-all objective must remain incomplete.
            MoveResult result = session.ExecuteMove(
                new MoveIntent("red_source", "trail_blue"));

            Assert.That(result.Succeeded, Is.True);
            Assert.That(session.CreateSnapshot().Blobs.Count, Is.EqualTo(1));
            Assert.That(session.CreateSnapshot().IsComplete, Is.False);
        }

        private sealed class ConsumingMergeStrategy : IMergeStrategy
        {
            public CollisionPlan BuildPlan(MoveContext context)
            {
                return CollisionPlan.ConsumeMover(
                    new RemoveBlobEffect(context.Target),
                    new RemoveBlobEffect(context.Source));
            }
        }

        private static GameSession CreateSession(
            string levelId,
            int width,
            int height,
            params BlobDefinition[] blobs)
        {
            return new GameSession(new LevelDefinition(
                levelId,
                LevelDefinition.CurrentSchemaVersion,
                width,
                height,
                blobs,
                new TileDefinition[0]));
        }

        private static NormalBlobDefinition NormalBlob(
            string id,
            BlobColor color,
            int x,
            int y)
        {
            return new NormalBlobDefinition(id, new GridPosition(x, y), color);
        }

        private static TrailBlobDefinition TrailBlob(
            string id,
            BlobColor color,
            BlobColor trailColor,
            int x,
            int y)
        {
            return new TrailBlobDefinition(
                id,
                new GridPosition(x, y),
                color,
                trailColor);
        }
    }
}
