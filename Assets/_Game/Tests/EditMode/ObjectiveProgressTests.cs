using System;
using System.Collections.Generic;
using Blobs.Application;
using Blobs.Core;
using NUnit.Framework;

namespace Blobs.Tests.EditMode
{
    public sealed class ObjectiveProgressTests
    {
        [Test]
        public void EatingMatchingBlobDecrementsRemaining()
        {
            GameSession session = CreateSession(
                Items(Item(new BlobObjectiveTarget(BlobType.Normal, BlobColor.Red), 2)),
                new NormalBlobDefinition("eater", new GridPosition(0, 0), BlobColor.Blue),
                new NormalBlobDefinition("red-a", new GridPosition(1, 0), BlobColor.Red),
                new NormalBlobDefinition("red-b", new GridPosition(0, 1), BlobColor.Red),
                new NestBlobDefinition("nest", new GridPosition(2, 1), BlobColor.Blue));

            Assert.That(session.Objectives.Items[0].Remaining, Is.EqualTo(2));

            Move(session, "eater", "red-a");

            Assert.That(session.Objectives.Items[0].Remaining, Is.EqualTo(1));
            Assert.That(session.IsComplete, Is.False);
            Assert.That(session.CreateSnapshot().Objectives.Items[0].Remaining, Is.EqualTo(1));
        }

        [Test]
        public void EatingNonMatchingColorDoesNotCount()
        {
            GameSession session = CreateSession(
                Items(Item(new BlobObjectiveTarget(BlobType.Normal, BlobColor.Red), 1)),
                new NormalBlobDefinition("eater", new GridPosition(0, 0), BlobColor.Red),
                new NormalBlobDefinition("green", new GridPosition(1, 0), BlobColor.Green),
                new NestBlobDefinition("nest", new GridPosition(2, 1), BlobColor.Red));

            Move(session, "eater", "green");

            Assert.That(session.Objectives.Items[0].Remaining, Is.EqualTo(1));
        }

        [Test]
        public void NestingCountsForBothTheMonsterAndTheNest()
        {
            GameSession session = CreateSession(
                Items(
                    Item(new BlobObjectiveTarget(BlobType.Normal, BlobColor.Red), 1),
                    Item(new BlobObjectiveTarget(BlobType.Nest, BlobColor.Red), 1)),
                new NormalBlobDefinition("monster", new GridPosition(0, 0), BlobColor.Red),
                new NestBlobDefinition("nest", new GridPosition(1, 0), BlobColor.Red));

            MoveResult result = Move(session, "monster", "nest");

            Assert.That(result.Succeeded, Is.True);
            Assert.That(session.Objectives.Items[0].Remaining, Is.Zero);
            Assert.That(session.Objectives.Items[1].Remaining, Is.Zero);
            Assert.That(session.IsComplete, Is.True);
        }

        [Test]
        public void TypeFilterSeparatesNormalMonstersFromNests()
        {
            GameSession session = CreateSession(
                Items(Item(new BlobObjectiveTarget(BlobType.Nest, BlobColor.Blue), 1)),
                new NormalBlobDefinition("eater", new GridPosition(0, 0), BlobColor.Red),
                new NormalBlobDefinition("blue", new GridPosition(1, 0), BlobColor.Blue),
                new NestBlobDefinition("nest", new GridPosition(2, 1), BlobColor.Blue));

            Move(session, "eater", "blue");

            Assert.That(session.Objectives.Items[0].Remaining, Is.EqualTo(1));
        }

        [Test]
        public void UndoAndRestartRestoreCounts()
        {
            GameSession session = CreateSession(
                Items(Item(new BlobObjectiveTarget(BlobType.Normal, BlobColor.Red), 2)),
                new NormalBlobDefinition("eater", new GridPosition(0, 0), BlobColor.Blue),
                new NormalBlobDefinition("red-a", new GridPosition(1, 0), BlobColor.Red),
                new NormalBlobDefinition("red-b", new GridPosition(0, 1), BlobColor.Red),
                new NestBlobDefinition("nest", new GridPosition(2, 1), BlobColor.Blue));

            Move(session, "eater", "red-a");
            Assert.That(session.Undo(), Is.True);
            Assert.That(session.Objectives.Items[0].Remaining, Is.EqualTo(2));

            Move(session, "eater", "red-a");
            session.Restart();
            Assert.That(session.Objectives.Items[0].Remaining, Is.EqualTo(2));
        }

        [Test]
        public void LevelWithoutItemsKeepsClearBoardCompletion()
        {
            GameSession session = CreateSession(
                Array.Empty<LevelObjectiveItemDefinition>(),
                new NormalBlobDefinition("monster", new GridPosition(0, 0), BlobColor.Red),
                new NestBlobDefinition("nest", new GridPosition(1, 0), BlobColor.Red));

            Move(session, "monster", "nest");

            Assert.That(session.Objectives.HasItems, Is.False);
            Assert.That(session.IsComplete, Is.True);
        }

        private static MoveResult Move(GameSession session, string sourceId, string targetId)
        {
            return session.ExecuteMove(new MoveIntent(
                session.CurrentState.GetBlob(sourceId),
                session.CurrentState.GetBlob(targetId)));
        }

        private static LevelObjectiveItemDefinition Item(ObjectiveTarget target, int amount)
        {
            return new LevelObjectiveItemDefinition(target, amount);
        }

        private static LevelObjectiveItemDefinition[] Items(params LevelObjectiveItemDefinition[] items)
        {
            return items;
        }

        private static GameSession CreateSession(
            IReadOnlyList<LevelObjectiveItemDefinition> items,
            params BlobDefinition[] blobs)
        {
            return new GameSession(new LevelDefinition(
                "objective-test",
                LevelDefinition.CurrentSchemaVersion,
                3,
                2,
                blobs,
                Array.Empty<TileDefinition>(),
                objective: new LevelObjectiveDefinition(items)));
        }
    }
}
