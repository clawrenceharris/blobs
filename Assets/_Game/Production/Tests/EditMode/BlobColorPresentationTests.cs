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
            "Assets/_Game/Production/Content/Presentation/LevelColorPalette_New.asset";
        private const string NormalPrefabPath =
            "Assets/_Game/Production/Prefabs/Blobs/PF_NormalBlob.prefab";
        private const string TrailPrefabPath =
            "Assets/_Game/Production/Prefabs/Blobs/PF_TrailBlob Variant.prefab";
        private const string FlagPrefabPath =
            "Assets/_Game/Production/Prefabs/Blobs/PF_FlagBlob.prefab";

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
            LevelColorPaletteAsset palette = LoadPalette();

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
            LevelColorPaletteAsset palette = LoadPalette();
            BlobView view = InstantiateView(NormalPrefabPath);
            BlobState state = Blob("normal", BlobType.Normal, BlobColor.Red);
            SpriteRenderer body = FindRenderer(view, "Body");
            Material authoredMaterial = body.sharedMaterial;

            view.Initialize(state, palette, 1f, Vector2.zero);

            AssertShaderColors(body, palette.GetRequired(BlobColor.Red));
            Assert.That(body.sharedMaterial, Is.SameAs(authoredMaterial));
            Assert.That(body.sharedMaterial.HasProperty("_BaseColor"), Is.True);
            Assert.That(body.sharedMaterial.HasProperty("_ShadowColor"), Is.True);
            Assert.That(body.sharedMaterial.HasProperty("_HighlightColor"), Is.True);
        }

        [Test]
        public void TrailBlobUsesPrimaryColorForBodyAndTrailColorForPuddle()
        {
            LevelColorPaletteAsset palette = LoadPalette();
            BlobView view = InstantiateView(TrailPrefabPath);
            BlobState state = new BlobState(
                "trail",
                BlobType.Trail,
                new GridPosition(0, 0))
            .WithColor(BlobColor.Red)
            .WithTrail(BlobColor.Blue);
            SpriteRenderer body = FindRenderer(view, "Body");
            SpriteRenderer puddle = FindRenderer(view, "Puddle");
            Material authoredBodyMaterial = body.sharedMaterial;
            Material authoredPuddleMaterial = puddle.sharedMaterial;

            view.Initialize(state, palette, 1f, Vector2.zero);

            AssertShaderColors(
                body,
                palette.GetRequired(BlobColor.Red));
            AssertShaderColors(
                puddle,
                palette.GetRequired(BlobColor.Blue));
            Assert.That(body.sharedMaterial, Is.SameAs(authoredBodyMaterial));
            Assert.That(puddle.sharedMaterial, Is.SameAs(authoredPuddleMaterial));
            Assert.That(
                view.GetComponentInChildren<TrailBlobColorBinding>(true),
                Is.Not.Null);
        }

        [Test]
        public void FlagBlobUsesFlatPaletteColorOnCheckersAndFinial()
        {
            LevelColorPaletteAsset palette = LoadPalette();
            BlobView view = InstantiateView(FlagPrefabPath);
            BlobState state = Blob("flag", BlobType.Flag, BlobColor.Purple);
            SpriteRenderer checkers = FindRenderer(view, "Checkers");
            SpriteRenderer finial = FindRenderer(view, "Finial");
            Material authoredCheckersMaterial = checkers.sharedMaterial;
            Material authoredFinialMaterial = finial.sharedMaterial;

            view.Initialize(state, palette, 1f, Vector2.zero);

            Color color = palette.GetRequired(BlobColor.Purple).BaseColor;
            var expected = new BlobShaderColors(color, color, color);
            AssertShaderColors(checkers, expected);
            AssertShaderColors(finial, expected);
            Assert.That(checkers.sharedMaterial, Is.SameAs(authoredCheckersMaterial));
            Assert.That(finial.sharedMaterial, Is.SameAs(authoredFinialMaterial));
        }

        private LevelColorPaletteAsset LoadPalette()
        {
            LevelColorPaletteAsset palette =
                AssetDatabase.LoadAssetAtPath<LevelColorPaletteAsset>(PalettePath);
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
            return new BlobState(id, type, new GridPosition(0, 0))
            .WithColor(color);
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
