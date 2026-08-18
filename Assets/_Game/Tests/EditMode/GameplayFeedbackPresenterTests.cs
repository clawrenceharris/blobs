using System.Collections.Generic;
using Blobs.Application;
using Blobs.Core;
using Blobs.Input;
using Blobs.Presentation;
using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace Blobs.Tests.EditMode
{
    public sealed class GameplayFeedbackPresenterTests
    {
        private readonly List<GameObject> _createdObjects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            for (int i = _createdObjects.Count - 1; i >= 0; i--)
            {
                if (_createdObjects[i] != null)
                    Object.DestroyImmediate(_createdObjects[i]);
            }

            _createdObjects.Clear();
        }

        [TestCase(MoveFailureReason.SourceOrTargetMissing, "That blob is no longer there.")]
        [TestCase(MoveFailureReason.SameBlob, "Choose a different blob.")]
        [TestCase(MoveFailureReason.SourceCannotMove, "That blob can't move.")]
        [TestCase(MoveFailureReason.NotAligned, "Use the same row or column.")]
        [TestCase(MoveFailureReason.BlockedPath, "Another blob is in the way.")]
        [TestCase(MoveFailureReason.UnsupportedInteraction, "Those blobs can't merge.")]
        [TestCase(
            MoveFailureReason.NormalMergeRequiresDifferentColors,
            "Normal blobs need different colors.")]
        [TestCase(MoveFailureReason.FlagRequiresMatchingColor, "Match the flag's color.")]
        [TestCase(MoveFailureReason.FlagRequiresNoOtherBlobs, "Clear the other blobs first.")]
        public void MessageForMapsFailureToPlayerFriendlyCopy(
            MoveFailureReason reason,
            string expected)
        {
            Assert.That(GameplayFeedbackPresenter.MessageFor(reason), Is.EqualTo(expected));
        }

        [Test]
        public void InputAdapterPublishesTheSelectionResultItReturns()
        {
            var expected = BlobSelectionResult.Rejected(MoveFailureReason.SourceCannotMove);
            var commands = new FakeCommands(expected);
            GameplayInputAdapter input = Create("Input").AddComponent<GameplayInputAdapter>();
            BlobSelectionResult published = null;
            input.BlobSelectionResolved += result => published = result;
            input.Initialize(commands, 1f);

            BlobSelectionResult returned = input.SelectBlobAt(new GridPosition(0, 0));
            Assert.That(returned.SourceId, Is.EqualTo("blob_a"));
            Assert.That(returned.TargetId, Is.EqualTo("blob_b"));

            Assert.That(returned, Is.SameAs(expected));
            Assert.That(published, Is.SameAs(expected));
        }

        [Test]
        public void FailedSelectionDisplaysMappedMessage()
        {
            GameObject feedbackObject = Create("Feedback");
            feedbackObject.SetActive(false);
            var text = feedbackObject.AddComponent<TextMeshProUGUI>();
            var presenter = feedbackObject.AddComponent<GameplayFeedbackPresenter>();

            GameplayInputAdapter input = Create("Input").AddComponent<GameplayInputAdapter>();
            input.Initialize(
                new FakeCommands(
                    BlobSelectionResult.Rejected(MoveFailureReason.FlagRequiresNoOtherBlobs)),
                1f);

            feedbackObject.SetActive(true);
            presenter.Initialize(input);

            input.SelectBlobAt(new GridPosition(0, 0));

            Assert.That(text.text, Is.EqualTo("Clear the other blobs first."));

        }

        private GameObject Create(string name)
        {
            var gameObject = new GameObject(name);
            _createdObjects.Add(gameObject);
            return gameObject;
        }

        private sealed class FakeCommands : IGameplayCommands
        {
            private readonly BlobSelectionResult _result;

            public FakeCommands(BlobSelectionResult result)
            {
                _result = result;
            }

            public BlobSelectionResult SelectBlobAt(GridPosition position)
            {
                return _result;
            }

            public void Restart()
            {
            }
        }
    }
}
