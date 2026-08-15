using System.Collections.Generic;
using System.Linq;
using Blobs.Core;
using NUnit.Framework;

namespace Blobs.Tests.EditMode
{
    public sealed class StepTimelineAndCollisionContractTests
    {
        [Test]
        public void AdjacentNormalMergeIsASingleMergeStep()
        {
            MoveResult result = Resolve(
                Board(
                    Blob("red", BlobType.Normal, BlobColor.Red, 0, 0),
                    Blob("blue", BlobType.Normal, BlobColor.Blue, 1, 0)),
                "red",
                "blue");

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Steps.Count, Is.EqualTo(1));
            Assert.That(result.Steps[0].Kind, Is.EqualTo(MoveStepKind.Merge));
            Assert.That(result.Steps[0].Effects[0], Is.TypeOf<RemoveBlobEffect>());
            Assert.That(result.Steps[0].Effects[1], Is.TypeOf<MoveBlobEffect>());
        }

        [Test]
        public void FlattenedEffectsMatchConcatenatedStepEffects()
        {
            MoveResult result = Resolve(
                Board(
                    Blob("red", BlobType.Normal, BlobColor.Red, 0, 0),
                    Blob("blue", BlobType.Normal, BlobColor.Blue, 2, 0)),
                "red",
                "blue");

            IBoardEffect[] fromSteps = result.Steps
                .SelectMany(step => step.Effects)
                .ToArray();

            Assert.That(result.Effects, Is.EqualTo(fromSteps));
            Assert.That(result.Steps.Count, Is.EqualTo(2));
            Assert.That(result.Steps[0].Kind, Is.EqualTo(MoveStepKind.Traverse));
            Assert.That(result.Steps[1].Kind, Is.EqualTo(MoveStepKind.Merge));
        }

        [Test]
        public void FailedMoveLeavesEmptyStepsAndDoesNotMutateBoard()
        {
            BoardState board = Board(
                Blob("red_a", BlobType.Normal, BlobColor.Red, 0, 0),
                Blob("red_b", BlobType.Normal, BlobColor.Red, 2, 0));

            MoveResult result = new MoveResolver().Resolve(
                board,
                new MoveIntent("red_a", "red_b"));

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Steps, Is.Empty);
            Assert.That(result.Effects, Is.Empty);
            Assert.That(board.GetBlob("red_a").Position, Is.EqualTo(new GridPosition(0, 0)));
            Assert.That(board.BlobCount, Is.EqualTo(2));
        }

        [Test]
        public void MissingStrategyFailsWithUnsupportedInteraction()
        {
            var rules = new BlobRuleBook(
                new Dictionary<BlobType, BlobTraits>
                {
                    [BlobType.Normal] =
                        new BlobTraits(canBeSource: true, isClearable: true),
                    [BlobType.Flag] =
                        new BlobTraits(canBeSource: false, isClearable: false)
                },
                new Dictionary<MergeKey, IMergeStrategy>());

            MoveResult result = new MoveResolver(rules).Resolve(
                Board(
                    Blob("red", BlobType.Normal, BlobColor.Red, 0, 0),
                    Blob("flag", BlobType.Flag, BlobColor.Red, 1, 0)),
                new MoveIntent("red", "flag"));

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.FailureReason,
                Is.EqualTo(MoveFailureReason.UnsupportedInteraction));
        }

        [Test]
        public void FollowUpStepsAreAppendedAfterLocomotion()
        {
            var followUp = new MoveStep(
                MoveStepKind.Traverse,
                new IBoardEffect[]
                {
                    new MoveBlobEffect("ghost", new GridPosition(2, 0), new GridPosition(0, 0))
                });

            var rules = new BlobRuleBook(
                new Dictionary<BlobType, BlobTraits>
                {
                    [BlobType.Normal] =
                        new BlobTraits(canBeSource: true, isClearable: true)
                },
                new Dictionary<MergeKey, IMergeStrategy>
                {
                    [new MergeKey(BlobType.Normal, BlobType.Normal)] =
                        new FollowUpMergeStrategy(followUp)
                });

            BoardState board = Board(
                Blob("mover", BlobType.Normal, BlobColor.Red, 0, 0),
                Blob("target", BlobType.Normal, BlobColor.Blue, 1, 0),
                Blob("ghost", BlobType.Normal, BlobColor.Green, 2, 0));

            MoveResult result = new MoveResolver(rules).Resolve(
                board,
                new MoveIntent("mover", "target"));

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Steps.Count, Is.EqualTo(2));
            Assert.That(result.Steps[0].Kind, Is.EqualTo(MoveStepKind.Merge));
            Assert.That(result.Steps[1], Is.SameAs(followUp));
            Assert.That(board.GetBlob("ghost").Position, Is.EqualTo(new GridPosition(0, 0)));
            Assert.That(board.GetBlob("mover").Position, Is.EqualTo(new GridPosition(1, 0)));
        }

        [Test]
        public void NormalMergeStrategyContinuesWithoutLocomotionEffect()
        {
            BlobState source = Blob("red", BlobType.Normal, BlobColor.Red, 0, 0);
            BlobState target = Blob("blue", BlobType.Normal, BlobColor.Blue, 1, 0);
            CollisionPlan plan = new NormalMergeStrategy().BuildPlan(
                new MoveContext(Board(source, target), source, target));

            Assert.That(plan.Succeeded, Is.True);
            Assert.That(plan.ConsumesMover, Is.False);
            Assert.That(plan.FollowUpSteps, Is.Empty);
            Assert.That(plan.Effects.Count, Is.EqualTo(1));
            Assert.That(plan.Effects[0], Is.TypeOf<RemoveBlobEffect>());
        }

        [Test]
        public void NormalMergeStrategyFailsOnMatchingColor()
        {
            BlobState source = Blob("red_a", BlobType.Normal, BlobColor.Red, 0, 0);
            BlobState target = Blob("red_b", BlobType.Normal, BlobColor.Red, 1, 0);
            CollisionPlan plan = new NormalMergeStrategy().BuildPlan(
                new MoveContext(Board(source, target), source, target));

            Assert.That(plan.Succeeded, Is.False);
            Assert.That(
                plan.FailureReason,
                Is.EqualTo(MoveFailureReason.NormalMergeRequiresDifferentColors));
            Assert.That(plan.Effects, Is.Empty);
        }

        [Test]
        public void FlagMergeStrategyConsumesMoverWhenBoardHasOnlyTwoBlobs()
        {
            BlobState source = Blob("red", BlobType.Normal, BlobColor.Red, 0, 0);
            BlobState flag = Blob("flag", BlobType.Flag, BlobColor.Red, 1, 0);
            CollisionPlan plan = new FlagMergeStrategy().BuildPlan(
                new MoveContext(Board(source, flag), source, flag));

            Assert.That(plan.Succeeded, Is.True);
            Assert.That(plan.ConsumesMover, Is.True);
            Assert.That(plan.Effects.Single(), Is.TypeOf<MergeIntoFlagEffect>());
        }

        [Test]
        public void CollisionPlanWithFollowUpStepsPreservesConsumeFlag()
        {
            var step = new MoveStep(MoveStepKind.Traverse, new IBoardEffect[0]);
            CollisionPlan plan = CollisionPlan
                .ConsumeMover(new RemoveBlobEffect(Blob("x", BlobType.Normal, BlobColor.Red, 0, 0)))
                .WithFollowUpSteps(new[] { step });

            Assert.That(plan.Succeeded, Is.True);
            Assert.That(plan.ConsumesMover, Is.True);
            Assert.That(plan.FollowUpSteps.Single(), Is.SameAs(step));
        }

        [Test]
        public void TrailAdjacentMergeSpawnsOnlyOnOrigin()
        {
            MoveResult result = Resolve(
                Board(
                    Trail("trail", BlobColor.Red, BlobColor.Blue, 0, 0),
                    Blob("green", BlobType.Normal, BlobColor.Green, 1, 0)),
                "trail",
                "green");

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Steps.Count, Is.EqualTo(1));
            Assert.That(result.Steps[0].Kind, Is.EqualTo(MoveStepKind.Merge));

            SpawnBlobEffect spawn = result.Effects.OfType<SpawnBlobEffect>().Single();
            Assert.That(spawn.At, Is.EqualTo(new GridPosition(0, 0)));
            Assert.That(spawn.Blob.Color, Is.EqualTo(BlobColor.Blue));
            Assert.That(spawn.Blob.Type, Is.EqualTo(BlobType.Normal));
        }

        [Test]
        public void AdjacentTrailCaptureIntoMatchingFlagConsumesMoverAndSpawnsAtOrigin()
        {
            BoardState board = Board(
                Trail("trail", BlobColor.Purple, BlobColor.Blue, 0, 0),
                Blob("flag", BlobType.Flag, BlobColor.Purple, 1, 0));

            MoveResult result = new MoveResolver().Resolve(
                board,
                new MoveIntent("trail", "flag"));

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Steps.Count, Is.EqualTo(1));
            Assert.That(result.Steps[0].Kind, Is.EqualTo(MoveStepKind.Merge));
            Assert.That(board.GetBlob("trail"), Is.Null);
            Assert.That(board.GetBlob("flag").Position, Is.EqualTo(new GridPosition(1, 0)));

            SpawnBlobEffect spawn = result.Effects.OfType<SpawnBlobEffect>().Single();
            Assert.That(spawn.At, Is.EqualTo(new GridPosition(0, 0)));
            Assert.That(board.GetBlobAt(new GridPosition(0, 0)).Color, Is.EqualTo(BlobColor.Blue));
        }

        [Test]
        public void TrailSpawnsOnThePathPreventFlagCaptureInTheSameMove()
        {
            // Traverse spawns a leftover before the flag cell, so BlobCount is no
            // longer 2 when the Flag strategy runs.
            BoardState board = Board(
                Trail("trail", BlobColor.Purple, BlobColor.Blue, 0, 0),
                Blob("flag", BlobType.Flag, BlobColor.Purple, 2, 0));

            MoveResult result = new MoveResolver().Resolve(
                board,
                new MoveIntent("trail", "flag"));

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.FailureReason,
                Is.EqualTo(MoveFailureReason.FlagRequiresNoOtherBlobs));
            Assert.That(board.GetBlob("trail").Position, Is.EqualTo(new GridPosition(0, 0)));
            Assert.That(board.BlobCount, Is.EqualTo(2));
        }

        [Test]
        public void TrailMoveBehaviorSkipsMergeSitesAndRequiresTrailColor()
        {
            var behavior = new TrailMoveBehavior();
            var effects = new List<IBoardEffect>();
            BlobState trail = Trail("trail", BlobColor.Red, BlobColor.Blue, 0, 0);

            behavior.OnTileDeparted(
                new MoveBehaviorContext(
                    Board(trail),
                    trail,
                    new GridPosition(0, 0),
                    departedTileWasMergeSite: true,
                    new SequentialBlobIdFactory()),
                effects);

            Assert.That(effects, Is.Empty);

            BlobState withoutTrailColor = Blob("plain", BlobType.Trail, BlobColor.Red, 0, 0);
            behavior.OnTileDeparted(
                new MoveBehaviorContext(
                    Board(withoutTrailColor),
                    withoutTrailColor,
                    new GridPosition(0, 0),
                    departedTileWasMergeSite: false,
                    new SequentialBlobIdFactory()),
                effects);

            Assert.That(effects, Is.Empty);
        }

        [Test]
        public void SequentialBlobIdFactorySkipsExistingIds()
        {
            BoardState board = Board(
                Blob("trail-1", BlobType.Normal, BlobColor.Blue, 1, 0));
            var factory = new SequentialBlobIdFactory();

            Assert.That(factory.CreateId(board, "trail"), Is.EqualTo("trail-2"));
            Assert.That(factory.CreateId(board, "trail"), Is.EqualTo("trail-3"));
        }

        [Test]
        public void TrailColorSurvivesWithPosition()
        {
            BlobState moved = Trail("trail", BlobColor.Red, BlobColor.Yellow, 0, 0)
                .WithPosition(new GridPosition(2, 1));

            Assert.That(moved.TrailColor, Is.EqualTo(BlobColor.Yellow));
            Assert.That(moved.IsClearable, Is.True);
            Assert.That(moved.Position, Is.EqualTo(new GridPosition(2, 1)));
        }

        [Test]
        public void MoveContextReportsIntermediateOccupantsAsNotFinalTarget()
        {
            var captured = new List<bool>();
            var rules = new BlobRuleBook(
                new Dictionary<BlobType, BlobTraits>
                {
                    [BlobType.Normal] =
                        new BlobTraits(canBeSource: true, isClearable: true)
                },
                new Dictionary<MergeKey, IMergeStrategy>
                {
                    [new MergeKey(BlobType.Normal, BlobType.Normal)] =
                        new RecordingMergeStrategy(captured)
                });

            MoveResult result = new MoveResolver(rules).Resolve(
                Board(
                    Blob("red", BlobType.Normal, BlobColor.Red, 0, 0),
                    Blob("blue", BlobType.Normal, BlobColor.Blue, 1, 0),
                    Blob("green", BlobType.Normal, BlobColor.Green, 2, 0)),
                new MoveIntent("red", "green"));

            Assert.That(result.Succeeded, Is.True);
            Assert.That(captured, Is.EqualTo(new[] { false, true }));
        }

        private static MoveResult Resolve(BoardState board, string sourceId, string targetId)
        {
            return new MoveResolver().Resolve(board, new MoveIntent(sourceId, targetId));
        }

        private static BoardState Board(params BlobState[] blobs)
        {
            return new BoardState(4, 2, blobs, new TileState[0]);
        }

        private static BlobState Blob(
            string id,
            BlobType type,
            BlobColor color,
            int x,
            int y)
        {
            return new BlobState(id, type, color, new GridPosition(x, y));
        }

        private static BlobState Trail(
            string id,
            BlobColor color,
            BlobColor trailColor,
            int x,
            int y)
        {
            return new BlobState(
                id,
                BlobType.Trail,
                color,
                new GridPosition(x, y),
                trailColor);
        }

        private sealed class FollowUpMergeStrategy : IMergeStrategy
        {
            private readonly MoveStep _followUp;

            public FollowUpMergeStrategy(MoveStep followUp)
            {
                _followUp = followUp;
            }

            public CollisionPlan BuildPlan(MoveContext context)
            {
                return CollisionPlan
                    .Continue(new RemoveBlobEffect(context.Target))
                    .WithFollowUpSteps(new[] { _followUp });
            }
        }

        private sealed class RecordingMergeStrategy : IMergeStrategy
        {
            private readonly List<bool> _isFinalTarget;

            public RecordingMergeStrategy(List<bool> isFinalTarget)
            {
                _isFinalTarget = isFinalTarget;
            }

            public CollisionPlan BuildPlan(MoveContext context)
            {
                _isFinalTarget.Add(context.IsFinalTarget);
                return CollisionPlan.Continue(new RemoveBlobEffect(context.Target));
            }
        }
    }
}
