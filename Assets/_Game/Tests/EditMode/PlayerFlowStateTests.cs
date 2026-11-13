using Blobs.Application;
using NUnit.Framework;

namespace Blobs.Tests.EditMode
{
    public sealed class PlayerFlowStateTests
    {
        [TearDown]
        public void TearDown()
        {
            PlayerFlowState.Clear();
        }

        [Test]
        public void TryConsumePendingLevelIndex_ReturnsFalseWhenEmpty()
        {
            Assert.That(PlayerFlowState.TryConsumePendingLevelIndex(out int index), Is.False);
            Assert.That(index, Is.EqualTo(0));
        }

        [Test]
        public void TryConsumePendingLevelIndex_ReturnsSetIndexOnce()
        {
            PlayerFlowState.SetPendingLevelIndex(3);

            Assert.That(PlayerFlowState.TryConsumePendingLevelIndex(out int first), Is.True);
            Assert.That(first, Is.EqualTo(3));
            Assert.That(PlayerFlowState.TryConsumePendingLevelIndex(out _), Is.False);
        }

        [Test]
        public void Clear_DiscardsPendingIndex()
        {
            PlayerFlowState.SetPendingLevelIndex(1);
            PlayerFlowState.Clear();

            Assert.That(PlayerFlowState.TryConsumePendingLevelIndex(out _), Is.False);
        }
    }
}
