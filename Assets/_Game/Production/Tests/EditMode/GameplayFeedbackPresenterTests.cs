using System.Collections.Generic;
using Blobs.Application;
using Blobs.Core;
using Blobs.Input;
using Blobs.Presentation;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Blobs.Tests.EditMode
{
    public sealed class GameplayFeedbackPresenterTests
    {
        private const string CatalogPath =
            "Assets/_Game/Production/Content/Presentation/FailureFeedbackCatalog.asset";

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

        [TestCase(MoveFailureReason.NotAligned, "Blobs must share the same column or row to merge.")]
        [TestCase(MoveFailureReason.SourceCannotMove, "That blob can't move.")]
        [TestCase(MoveFailureReason.PathBlocked, "Path is blocked.")]
        [TestCase(MoveFailureReason.UnsupportedInteraction, "Those blobs can't merge.")]
        [TestCase(
            MoveFailureReason.NormalMergeRequiresDifferentColors,
            "Normal blobs need different colors.")]
        [TestCase(MoveFailureReason.FlagRequiresMatchingColor, "Match the flag's color.")]
        [TestCase(MoveFailureReason.FlagRequiresNoOtherBlobs, "Clear the other blobs first.")]
        [TestCase(MoveFailureReason.MoveTimeout, "That move took too long.")]
        public void CatalogMapsVisibleFailureToPlayerFriendlyCopy(
            MoveFailureReason reason,
            string expected)
        {
            FailureFeedbackCatalogAsset catalog = LoadCatalog();

            Assert.That(catalog.TryGetVisibleMessage(reason, out string message), Is.True);
            Assert.That(message, Is.EqualTo(expected));
        }

        [TestCase(MoveFailureReason.None)]
        [TestCase(MoveFailureReason.SourceOrTargetMissing)]
        [TestCase(MoveFailureReason.SameBlob)]
        public void CatalogKeepsSelectionStageFailuresSilent(MoveFailureReason reason)
        {
            FailureFeedbackCatalogAsset catalog = LoadCatalog();

            Assert.That(catalog.TryGetVisibleMessage(reason, out string message), Is.False);
            Assert.That(message, Is.Empty);
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

            Assert.That(returned, Is.SameAs(expected));
            Assert.That(published, Is.SameAs(expected));
        }

        [Test]
        public void FailedMoveDisplaysMappedMessage()
        {
            GameObject feedbackObject = Create("Feedback");
            feedbackObject.SetActive(false);
            var text = feedbackObject.AddComponent<TextMeshProUGUI>();
            var presenter = feedbackObject.AddComponent<GameplayFeedbackPresenter>();
            var serializedPresenter = new SerializedObject(presenter);
            serializedPresenter.FindProperty("feedbackCatalog").objectReferenceValue = LoadCatalog();
            serializedPresenter.ApplyModifiedPropertiesWithoutUndo();

            GameplayInputAdapter input = Create("Input").AddComponent<GameplayInputAdapter>();
            input.Initialize(
                new FakeCommands(
                    BlobSelectionResult.Move(MoveResult.Failed("source", "flag", MoveFailureReason.FlagRequiresNoOtherBlobs))),
                1f);

            feedbackObject.SetActive(true);
            presenter.Initialize(input);

            input.SelectBlobAt(new GridPosition(0, 0));

            Assert.That(text.text, Is.EqualTo("Clear the other blobs first."));
        }

        [Test]
        public void PresenterWithoutCatalogFailsClearlyAndDoesNotSubscribe()
        {
            GameObject feedbackObject = Create("Feedback Without Catalog");
            var text = feedbackObject.AddComponent<TextMeshProUGUI>();
            var presenter = feedbackObject.AddComponent<GameplayFeedbackPresenter>();
            GameplayInputAdapter input = Create("Input").AddComponent<GameplayInputAdapter>();
            input.Initialize(
                new FakeCommands(
                    BlobSelectionResult.Rejected(MoveFailureReason.SourceCannotMove)),
                1f);

            LogAssert.Expect(
                LogType.Error,
                "Gameplay feedback on 'Feedback Without Catalog' cannot initialize without a " +
                "failure feedback catalog.");

            presenter.Initialize(input);
            input.SelectBlobAt(new GridPosition(0, 0));

            Assert.That(text.text, Is.Empty);
        }

        private static FailureFeedbackCatalogAsset LoadCatalog()
        {
            FailureFeedbackCatalogAsset catalog =
                AssetDatabase.LoadAssetAtPath<FailureFeedbackCatalogAsset>(CatalogPath);
            Assert.That(catalog, Is.Not.Null);
            return catalog;
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
