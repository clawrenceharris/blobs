using System.Linq;
using Blobs.Core;
using Blobs.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Blobs.Tests.EditMode
{
    public sealed class GhostPrefabTests
    {
        [Test]
        public void CatalogsResolveGhostAndSigilPrefabs()
        {
            var blobs = AssetDatabase.LoadAssetAtPath<ViewCatalogAsset>(
                "Assets/_Game/Content/Presentation/BlobViewCatalog.asset");
            var tiles = AssetDatabase.LoadAssetAtPath<ViewCatalogAsset>(
                "Assets/_Game/Content/Presentation/TileViewCatalog.asset");
            Assert.That(blobs.GetRequiredBlobPrefab(BlobType.Ghost).GetComponent<BlobRenderer>().FadeableVisual,
                Is.Not.Null);
        }

        [Test]
        public void AuthoredGhostFadesBodyAndShadowWhilePreservingHalo()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Game/Prefabs/Blobs/PF_GhostBlob.prefab");
            Assert.That(prefab, Is.Not.Null);
            var instance = Object.Instantiate(prefab);
            try
            {
                var renderer = instance.GetComponent<BlobRenderer>();
                Assert.That(renderer.BaseRenderer, Is.Not.Null, "Preserve old serialized renderer bindings.");
                var fade = renderer.FadeableVisual;
                Assert.That(fade, Is.Not.Null);
                var sprites = instance.GetComponentsInChildren<SpriteRenderer>();
                var halo = sprites.Single(x => x.name == "Halo");
                var shadow = sprites.Single(x => x.name == "Shadow");
                float shadowAlpha = shadow.color.a;
                fade.FadeTo(0f, 0f).GetAwaiter().GetResult();
                Assert.That(renderer.BaseRenderer.color.a, Is.Zero);
                Assert.That(shadow.color.a, Is.Zero);
                Assert.That(halo.color.a, Is.EqualTo(1f));
                fade.Restore();
                Assert.That(renderer.BaseRenderer.color.a, Is.EqualTo(1f));
                Assert.That(shadow.color.a, Is.EqualTo(shadowAlpha));
            }
            finally { Object.DestroyImmediate(instance); }
        }
    }
}
