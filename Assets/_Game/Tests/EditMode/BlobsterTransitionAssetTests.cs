using Blobs.Content;
using Blobs.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Blobs.Tests.EditMode
{
    public sealed class BlobsterTransitionAssetTests
    {
        [Test]
        public void DefaultAndWorldStyles_HaveRequiredVisualReferences()
        {
            BlobsterTransitionStyle style = Resources.Load<BlobsterTransitionStyle>("BlobsterTransition/Default");
            WorldDefinitionAsset normalWorld = AssetDatabase.LoadAssetAtPath<WorldDefinitionAsset>(
                "Assets/_Game/Content/Worlds/World_01.asset");
            WorldDefinitionAsset ghostWorld = AssetDatabase.LoadAssetAtPath<WorldDefinitionAsset>(
                "Assets/_Game/Content/Worlds/World_02.asset");

            Assert.That(style, Is.Not.Null);
            Assert.That(style.Presentation.EyesSprite, Is.Null, "The default Home-to-Worlds transition must be neutral.");
            Assert.That(style.MaskMaterial, Is.Not.Null);
            Assert.That(style.MaskMaterial.shader, Is.Not.Null);
            Assert.That(style.MaskMaterial.shader.name, Is.EqualTo("Blobs/UI/Blobster Transition Mask"));
            Assert.That(style.MaskMaterial.HasProperty("_LipWidthPixels"), Is.True);
            Assert.That(style.Presentation.LipWidthPixels, Is.EqualTo(20f));
            Assert.That(style.Presentation.MaskCenter, Is.EqualTo(new Vector2(0.5f, 0.32f)));
            Assert.That(style.Presentation.FaceMouthOffset, Is.EqualTo(new Vector2(-0.65f, 0.8f)));
            Assert.That(style.Presentation.FacePosition, Is.EqualTo(new Vector2(-60f, 220f)));
            Assert.That(style.Presentation.FaceSize, Is.EqualTo(new Vector2(360f, 360f)));
            Assert.That(style.Presentation.ClosedMouthRadii, Is.EqualTo(new Vector2(0.06f, 0.006f)));
            Assert.That(style.Presentation.BackgroundColor.a, Is.GreaterThan(0f));
            Assert.That(style.Presentation.CloseDuration, Is.GreaterThan(0f));
            Assert.That(style.Presentation.OpenDuration, Is.GreaterThan(0f));
            Assert.That(style.Presentation.OpeningFeedback, Is.Not.Null);

            Assert.That(normalWorld, Is.Not.Null);
            Assert.That(normalWorld.Transition, Is.Not.Null);
            Assert.That(normalWorld.Transition.EyesSprite, Is.Not.Null);
            Assert.That(normalWorld.Transition.OverrideComposition, Is.True);
            Assert.That(normalWorld.Transition.FaceSize.x, Is.GreaterThan(0f));
            Assert.That(normalWorld.Transition.FaceSize.y, Is.GreaterThan(0f));

            Assert.That(ghostWorld, Is.Not.Null);
            Assert.That(ghostWorld.Transition, Is.Not.Null);
            Assert.That(ghostWorld.Transition.EyesSprite, Is.Not.Null);
            Assert.That(ghostWorld.Transition.OverrideComposition, Is.True);
            Assert.That(ghostWorld.Transition.BackgroundColor, Is.EqualTo(Color.white));
            Assert.That(normalWorld.Transition.BackgroundColor,
                Is.Not.EqualTo(ghostWorld.Transition.BackgroundColor));
            Assert.That(normalWorld.Transition.OpeningFeedback, Is.Not.Null);
            Assert.That(ghostWorld.Transition.OpeningFeedback, Is.Not.Null);
        }
    }
}
