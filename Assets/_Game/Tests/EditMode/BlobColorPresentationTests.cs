using System;
using System.Collections.Generic;
using Blobs.Content;
using Blobs.Core;
using Blobs.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Blobs.Tests.EditMode
{
    public sealed class BlobColorPresentationTests
    {
        private const string PalettePath =
            "Assets/_Game/Content/Presentation/BlobColorPalette_Default.asset";
        private const string NormalPrefabPath =
            "Assets/_Game/Prefabs/Blobs/PF_NormalBlob.prefab";
        private const string TrailPrefabPath =
            "Assets/_Game/Prefabs/Blobs/PF_TrailBlob Variant.prefab";
        private const string FlagPrefabPath =
            "Assets/_Game/Prefabs/Blobs/PF_FlagBlob.prefab";

        private readonly List<UnityEngine.Object> _createdObjects = new();

        [TearDown]
        public void TearDown()
        {
            foreach (UnityEngine.Object createdObject in _createdObjects)
            {
                if (createdObject != null)
                    UnityEngine.Object.DestroyImmediate(createdObject);
            }

            _createdObjects.Clear();
        }

        [Test]
        public void PaletteDefinesThreeShaderColorsForEveryBlobColor()
        {
            BlobColorPaletteAsset palette = LoadPalette();

            foreach (BlobColor color in Enum.GetValues(typeof(BlobColor)))
            {
                BlobShaderColors colors = palette.GetRequired(color);

                Assert.That(colors, Is.Not.Null, color.ToString());
                Assert.That(colors.ShadowColor, Is.Not.EqualTo(colors.BaseColor), color.ToString());
                Assert.That(colors.HighlightColor, Is.Not.EqualTo(colors.BaseColor), color.ToString());
            }
        }

        [Test]
        public void NormalBlobAppliesPaletteToBodyShaderProperties()
        {
            BlobColorPaletteAsset palette = LoadPalette();
            BlobView view = InstantiateView(NormalPrefabPath);
            BlobState state = Blob("normal", BlobType.Normal, BlobColor.Red);

            view.Initialize(state, null, palette, 1f, Vector2.zero);

            SpriteRenderer body = FindRenderer(view, "Body");
            AssertShaderColors(body, palette.GetRequired(BlobColor.Red));
            Assert.That(body.sharedMaterial.HasProperty("_BaseColor"), Is.True);
            Assert.That(body.sharedMaterial.HasProperty("_ShadowColor"), Is.True);
            Assert.That(body.sharedMaterial.HasProperty("_HighlightColor"), Is.True);
        }

        [Test]
        public void TrailBlobUsesPrimaryColorForBodyAndTrailColorForPuddle()
        {
            BlobColorPaletteAsset palette = LoadPalette();
            BlobView view = InstantiateView(TrailPrefabPath);
            BlobState state = new BlobState(
                "trail",
                BlobType.Trail,
                new GridPosition(0, 0)).AddModel(new ColorBlobModel(BlobColor.Red)).AddModel(new TrailBlobModel(BlobColor.Blue));

            view.Initialize(state, null, palette, 1f, Vector2.zero);

            AssertShaderColors(
                FindRenderer(view, "Body"),
                palette.GetRequired(BlobColor.Red));
            AssertShaderColors(
                FindRenderer(view, "Puddle"),
                palette.GetRequired(BlobColor.Blue));
            Assert.That(
                view.GetComponentInChildren<TrailBlobColorBinding>(true),
                Is.Not.Null);
        }

        [Test]
        public void FlagBlobColorsCheckersAndFinialButNotBody()
        {
            BlobColorPaletteAsset palette = LoadPalette();
            BlobView view = InstantiateView(FlagPrefabPath);
            BlobState state = Blob("flag", BlobType.Flag, BlobColor.Purple);

            view.Initialize(state, null, palette, 1f, Vector2.zero);

            BlobShaderColors expected = palette.GetRequired(BlobColor.Purple);
            AssertShaderColors(FindRenderer(view, "Checkers"), expected);
            AssertShaderColors(FindRenderer(view, "Finial"), expected);

            var properties = new MaterialPropertyBlock();
            FindRenderer(view, "Body").GetPropertyBlock(properties);
            Assert.That(properties.isEmpty, Is.True);
        }

        private BlobColorPaletteAsset LoadPalette()
        {
            BlobColorPaletteAsset palette =
                AssetDatabase.LoadAssetAtPath<BlobColorPaletteAsset>(PalettePath);
            Assert.That(palette, Is.Not.Null);
            return palette;
        }

        private BlobView InstantiateView(string path)
        {
            BlobView prefab = AssetDatabase.LoadAssetAtPath<BlobView>(path);
            Assert.That(prefab, Is.Not.Null, path);

            BlobView instance = UnityEngine.Object.Instantiate(prefab);
            _createdObjects.Add(instance.gameObject);
            return instance;
        }

        private static BlobState Blob(string id, BlobType type, BlobColor color)
        {
            return new BlobState(id, type, new GridPosition(0, 0)).AddModel(new ColorBlobModel(color));
        }

        private static SpriteRenderer FindRenderer(BlobView view, string objectName)
        {
            foreach (SpriteRenderer renderer in view.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (renderer.name == objectName)
                    return renderer;
            }

            Assert.Fail($"Renderer '{objectName}' was not found in {view.name}.");
            return null;
        }

        private static void AssertShaderColors(
            SpriteRenderer renderer,
            BlobShaderColors expected)
        {
            var properties = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(properties);

            Assert.That(properties.GetColor("_BaseColor"), Is.EqualTo(expected.BaseColor));
            Assert.That(properties.GetColor("_ShadowColor"), Is.EqualTo(expected.ShadowColor));
            Assert.That(properties.GetColor("_HighlightColor"), Is.EqualTo(expected.HighlightColor));
            Assert.That(renderer.color.r, Is.EqualTo(1f));
            Assert.That(renderer.color.g, Is.EqualTo(1f));
            Assert.That(renderer.color.b, Is.EqualTo(1f));
        }
    }
}
