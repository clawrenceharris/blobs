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
            "Assets/_Game/Content/Presentation/LevelColorPalette_New.asset";
        private const string NormalPrefabPath =
            "Assets/_Game/Prefabs/PF_Blob_Normal.prefab";
        private const string TrailPrefabPath =
            "Assets/_Game/Prefabs/PF_Blob_Trail.prefab";
        private const string FlagPrefabPath =
            "Assets/_Game/Prefabs/PF_Blob_Flag.prefab";

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
        public void PaletteDefinesShaderColorsAndRampHsvForEveryBlobColor()
        {
            LevelColorPaletteAsset palette = LoadPalette();

            Assert.That(palette.SharedBlobMaterial, Is.Not.Null);
            Assert.That(palette.SharedBlobMaterial.IsKeywordEnabled("COLORRAMP_ON"), Is.True);
            Assert.That(palette.SharedBlobMaterial.IsKeywordEnabled("HSV_ON"), Is.True);
            Assert.That(palette.SharedBlobMaterial.GetTexture("_ColorRampTex"), Is.Not.Null);

            foreach (BlobColor color in Enum.GetValues(typeof(BlobColor)))
            {
                BlobShaderColors colors = palette.GetRequired(color);
                BlobRampHsv rampHsv = palette.GetRampHsv(color);

                Assert.That(colors, Is.Not.Null, color.ToString());
                Assert.That(colors.ShadowColor, Is.Not.EqualTo(colors.BaseColor), color.ToString());
                Assert.That(colors.HighlightColor, Is.Not.EqualTo(colors.BaseColor), color.ToString());
                Assert.That(rampHsv, Is.Not.Null, color.ToString());
                Assert.That(rampHsv.HueShift, Is.InRange(0f, 360f), color.ToString());
                Assert.That(rampHsv.Saturation, Is.GreaterThanOrEqualTo(0f), color.ToString());
                Assert.That(rampHsv.Brightness, Is.GreaterThanOrEqualTo(0f), color.ToString());
            }
        }

        [Test]
        public void NormalBlobUsesSharedRampMaterialAndPaletteHsv()
        {
            LevelColorPaletteAsset palette = LoadPalette();
            BlobView view = InstantiateView(NormalPrefabPath);
            BlobState state = Blob("normal", BlobType.Normal, BlobColor.Red);
            SpriteRenderer body = FindRenderer(view, "Body");

            view.Initialize(state, palette, 1f, Vector2.zero);

            AssertRampSkin(body, palette, BlobColor.Red);
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

            view.Initialize(state, palette, 1f, Vector2.zero);

            AssertRampSkin(body, palette, BlobColor.Red);
            AssertRampSkin(puddle, palette, BlobColor.Blue);
            Assert.That(
                view.GetComponentInChildren<TrailBlobColorBinding>(true),
                Is.Not.Null);
        }

        [Test]
        public void FlagBlobUsesSharedRampMaterialOnCheckersAndFinial()
        {
            LevelColorPaletteAsset palette = LoadPalette();
            BlobView view = InstantiateView(FlagPrefabPath);
            BlobState state = Blob("flag", BlobType.Flag, BlobColor.Purple);
            SpriteRenderer checkers = FindRenderer(view, "Checkers");
            SpriteRenderer finial = FindRenderer(view, "Finial");

            view.Initialize(state, palette, 1f, Vector2.zero);

            AssertRampSkin(checkers, palette, BlobColor.Purple);
            AssertRampSkin(finial, palette, BlobColor.Purple);
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

        private static void AssertRampSkin(
            SpriteRenderer renderer,
            LevelColorPaletteAsset palette,
            BlobColor color)
        {
            BlobRampHsv expected = palette.GetRampHsv(color);
            var properties = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(properties);

            Assert.That(renderer.sharedMaterial, Is.SameAs(palette.SharedBlobMaterial));
            Assert.That(properties.GetFloat("_HsvShift"), Is.EqualTo(expected.HueShift));
            Assert.That(properties.GetFloat("_HsvSaturation"), Is.EqualTo(expected.Saturation));
            Assert.That(properties.GetFloat("_HsvBright"), Is.EqualTo(expected.Brightness));
            Assert.That(renderer.color.r, Is.EqualTo(1f));
            Assert.That(renderer.color.g, Is.EqualTo(1f));
            Assert.That(renderer.color.b, Is.EqualTo(1f));
        }
    }
}
