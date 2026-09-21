using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using Blobs.Application;
using Blobs.Content;
using Blobs.Core;
using Blobs.Input;
using Blobs.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Blobs.Tests.PlayMode
{
    public sealed class PresentationCompositionTests : PresentationTestFixture
    {
        [UnityTest]
        public IEnumerator InstantiatedBlobCompositionInvokesEveryContactFeedbackChannel()
        {
            GameObject prefab = CreateGameObject("Composed Blob Prefab", active: false);
            prefab.AddComponent<BlobView>();
            RecordingContactFeedback originalFirst =
                prefab.AddComponent<RecordingContactFeedback>();
            RecordingContactFeedback originalSecond =
                prefab.AddComponent<RecordingContactFeedback>();

            GameObject instance = Object.Instantiate(prefab);
            _createdObjects.Add(instance);
            instance.name = "Composed Blob Instance";
            instance.SetActive(true);
            BlobView view = instance.GetComponent<BlobView>();
            view.Initialize(
                Blob("target", BlobColor.Green, 0, 0),
                CreatePalette(),
                1f,
                Vector2.zero);

            view.PlayContactFeedback(source: null);
            yield return null;

            RecordingContactFeedback[] feedback =
                instance.GetComponents<RecordingContactFeedback>();
            Assert.That(feedback, Has.Length.EqualTo(2));
            Assert.That(feedback[0].InvocationCount, Is.EqualTo(1));
            Assert.That(feedback[1].InvocationCount, Is.EqualTo(1));
            Assert.That(feedback[0].LastContext.Target, Is.SameAs(view));
            Assert.That(feedback[1].LastContext.Target, Is.SameAs(view));
            Assert.That(originalFirst.InvocationCount, Is.Zero);
            Assert.That(originalSecond.InvocationCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator ComposedSceneRootStartsSessionOnFirstFrame()
        {
            GameObject root = CreateGameObject("Runtime Scene Composition", active: false);
            root.AddComponent<MergeAnimationOrchestrator>();
            var surface = new GameObject("Board Surface");
            surface.transform.SetParent(root.transform, false);
            surface.AddComponent<BoardSurfaceView>();

            BoardPresenter board = root.AddComponent<BoardPresenter>();
            BlobAnimationSettingsAsset animationSettings = ConfigureAnimationSettings(root);
            GameplayInputAdapter input = root.AddComponent<GameplayInputAdapter>();
            GameplayCommandAdapter commands = root.AddComponent<GameplayCommandAdapter>();
            GameBootstrapper bootstrapper = root.AddComponent<GameBootstrapper>();
            LevelColorPaletteAsset palette = CreatePalette();
            LevelDefinitionAsset level = CreateStartupLevel(palette);
            ViewCatalogAsset viewCatalog = CreateAsset<ViewCatalogAsset>();
            BlobView blobPrefab = CreateCatalogBlobPrefab(animationSettings);
            SetPrivateField(viewCatalog, "blobEntries", new List<BlobViewCatalogEntry>
            {
                CatalogEntry(BlobType.Normal, blobPrefab),
                CatalogEntry(BlobType.Flag, blobPrefab)
            });

            SetPrivateField(board.GetComponent<BlobPresenter>(), "_viewCatalog", viewCatalog);
            SetPrivateField(bootstrapper, "boardPresenter", board);
            SetPrivateField(bootstrapper, "levelAsset", level);
            SetPrivateField(bootstrapper, "inputAdapter", input);
            SetPrivateField(bootstrapper, "commandAdapter", commands);

            int sessionStartedCount = 0;
            bootstrapper.SessionStarted += (_, _) => sessionStartedCount++;
            root.SetActive(true);

            yield return WaitUntil(() => bootstrapper.CurrentState != null);

            Assert.That(sessionStartedCount, Is.EqualTo(1));
            Assert.That(bootstrapper.CurrentState.CreateSnapshot().LevelId, Is.EqualTo("playmode-startup"));
            Assert.That(board.CurrentSnapshot, Is.Not.Null);
            Assert.That(board.VisibleBlobCount, Is.EqualTo(2));
            Assert.That(board.VisibleTileCount, Is.Zero);
            Assert.That(board.VisibleSurfaceCellCount, Is.EqualTo(2));
            Assert.That(
                input.SelectBlobAt(new GridPosition(2, 0)).HasSelection,
                Is.False);
        }

        private BlobView CreateCatalogBlobPrefab(BlobAnimationSettingsAsset animationSettings)
        {
            GameObject prefab = CreateGameObject("Catalog Blob Prefab", active: false);
            var visual = new GameObject("Visual");
            visual.transform.SetParent(prefab.transform, false);
            var view = prefab.AddComponent<BlobView>();
            var sortingGroup = prefab.AddComponent<SortingGroup>();
            var animator = prefab.AddComponent<BlobMotionAnimator>();
            SetPrivateField(animator, "_blobAnimationSettings", animationSettings);
            SetPrivateField(view, "_visualRoot", visual.transform);
            SetPrivateField(view, "_sortingGroup", sortingGroup);
            return view;
        }

        private static BlobViewCatalogEntry CatalogEntry(BlobType type, BlobView prefab)
        {
            var entry = new BlobViewCatalogEntry();
            SetPrivateField(entry, "type", type);
            SetPrivateField(entry, "prefab", prefab);
            return entry;
        }
    }
}
