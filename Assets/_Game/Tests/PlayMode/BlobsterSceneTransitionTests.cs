using System.Collections;
using System.Linq;
using System.Reflection;
using Blobs.Content;
using Blobs.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Blobs.Tests.PlayMode
{
    public sealed class BlobsterSceneTransitionTests
    {
        [UnityTearDown]
        public IEnumerator WaitForTransitionCleanup()
        {
            float timeout = Time.realtimeSinceStartup + 5f;
            while (BlobsterSceneTransition.IsRunning && Time.realtimeSinceStartup < timeout)
                yield return null;

            Assert.That(BlobsterSceneTransition.IsRunning, Is.False,
                "A transition was still running after the test cleanup timeout.");
        }

        [UnityTest]
        public IEnumerator Preview_RendersClosingAndOpeningMask()
        {
            BlobsterTransitionStyle style = Resources.Load<BlobsterTransitionStyle>("BlobsterTransition/Default");
            Assert.That(style, Is.Not.Null);
            Assert.That(BlobsterSceneTransition.PlayPreview(style), Is.True);

            yield return null;

            GameObject root = Resources.FindObjectsOfTypeAll<GameObject>()
                .First(item => item.name == "Blobster Scene Transition" && item.scene.IsValid());
            Canvas canvas = root.GetComponent<Canvas>();
            Image mask = root.GetComponentsInChildren<Image>(true)
                .First(item => item.name == "Stomach Mask");
            Vector2 firstRadii = mask.material.GetVector("_MouthRadii");
            float firstLipWidth = mask.material.GetFloat("_LipWidthPixels");

            Assert.That(canvas.enabled, Is.True);
            Assert.That(mask.material.shader.name, Is.EqualTo("Blobs/UI/Blobster Transition Mask"));
            Assert.That(firstLipWidth, Is.EqualTo(20f).Within(0.01f));
            Assert.That(firstRadii.x, Is.EqualTo(firstRadii.y).Within(0.0001f),
                "The main opening must begin as a true circle.");

            // Sample the next rendered frames, before the explicit circle-to-slit reshape begins.
            yield return null;
            yield return null;

            Vector2 laterRadii = mask.material.GetVector("_MouthRadii");
            Assert.That(laterRadii.x, Is.LessThan(firstRadii.x),
                "The closing mask should contract over rendered frames.");
            Assert.That(laterRadii.x, Is.EqualTo(laterRadii.y).Within(0.0001f),
                "The main growth phase must remain circular rather than stretching unevenly.");
            Assert.That(mask.material.GetFloat("_LipWidthPixels"),
                Is.EqualTo(firstLipWidth).Within(0.01f),
                "The shader lip must keep a constant pixel width while the mouth scales.");

            TMP_Text tip = root.GetComponentsInChildren<TMP_Text>(true)
                .First(item => item.name == "Game Tip");
            float tipTimeout = Time.realtimeSinceStartup + 2f;
            while ((!tip.enabled || tip.alpha < 0.5f || string.IsNullOrWhiteSpace(tip.text))
                   && Time.realtimeSinceStartup < tipTimeout)
                yield return null;

            Assert.That(tip.enabled, Is.True, "The covered screen should display a game tip.");
            Assert.That(tip.alpha, Is.GreaterThanOrEqualTo(0.5f), "The game tip should be readable.");
            Assert.That(tip.text, Is.Not.Empty);
            Assert.That(tip.fontSize, Is.GreaterThanOrEqualTo(36f));
            Assert.That(tip.alignment, Is.EqualTo(TextAlignmentOptions.Center));

            float timeout = Time.realtimeSinceStartup + 20f;
            while (BlobsterSceneTransition.IsRunning && Time.realtimeSinceStartup < timeout)
                yield return null;

            Assert.That(BlobsterSceneTransition.IsRunning, Is.False, "The preview did not complete before the timeout.");
            Assert.That(canvas.enabled, Is.False);
        }

        [UnityTest]
        public IEnumerator CoveredAction_RunsDuringTransitionAndRevealsAgain()
        {
            bool coveredActionRan = false;
            BlobsterTransitionStyle sourceStyle =
                Resources.Load<BlobsterTransitionStyle>("BlobsterTransition/Default");
            BlobsterTransitionStyle fastStyle = CreateFastStyle(sourceStyle);

            Assert.That(
                BlobsterSceneTransition.PlayCoveredAction(
                    () => coveredActionRan = true,
                    style: fastStyle),
                Is.True);
            Assert.That(coveredActionRan, Is.False,
                "The level switch must wait until the closing animation covers the screen.");

            float actionTimeout = Time.realtimeSinceStartup + 20f;
            while (!coveredActionRan && Time.realtimeSinceStartup < actionTimeout)
                yield return null;

            Assert.That(coveredActionRan, Is.True);

            float revealTimeout = Time.realtimeSinceStartup + 20f;
            while (BlobsterSceneTransition.IsRunning && Time.realtimeSinceStartup < revealTimeout)
                yield return null;

            Assert.That(BlobsterSceneTransition.IsRunning, Is.False);
            Object.Destroy(fastStyle);
        }

        [UnityTest]
        public IEnumerator WorldPresentation_OverridesDefaultColorAndFace()
        {
            BlobsterTransitionStyle sourceStyle =
                Resources.Load<BlobsterTransitionStyle>("BlobsterTransition/Default");
            BlobsterTransitionStyle fastStyle = CreateFastStyle(sourceStyle);
            Sprite eyesSprite = Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f));
            var presentation = new WorldTransitionPresentation();
            SetPrivateField(presentation, "eyesSprite", eyesSprite);
            SetPrivateField(presentation, "backgroundColor", Color.cyan);
            SetPrivateField(presentation, "overrideComposition", true);
            SetPrivateField(presentation, "maskCenter", new Vector2(0.25f, 0.65f));
            SetPrivateField(presentation, "faceFollowsMouth", false);
            SetPrivateField(presentation, "faceAnchor", new Vector2(0.4f, 0.7f));
            SetPrivateField(presentation, "facePosition", new Vector2(40f, -60f));
            SetPrivateField(presentation, "faceSize", new Vector2(500f, 600f));
            SetPrivateField(presentation, "faceRotation", 18f);

            Assert.That(BlobsterSceneTransition.PlayPreview(fastStyle, presentation), Is.True);
            yield return null;

            GameObject root = Resources.FindObjectsOfTypeAll<GameObject>()
                .First(item => item.name == "Blobster Scene Transition" && item.scene.IsValid());
            Image mask = root.GetComponentsInChildren<Image>(true)
                .First(item => item.name == "Stomach Mask");
            Image face = root.GetComponentsInChildren<Image>(true)
                .First(item => item.name == "Blobster Eye");

            Assert.That(mask.color, Is.EqualTo(Color.cyan));
            Assert.That((Vector2)mask.material.GetVector("_MaskCenter"),
                Is.EqualTo(new Vector2(0.25f, 0.65f)));
            Assert.That(face.sprite, Is.SameAs(eyesSprite));
            Assert.That(face.enabled, Is.True);
            Assert.That(face.rectTransform.anchorMin, Is.EqualTo(new Vector2(0.4f, 0.7f)));
            Assert.That(face.rectTransform.anchorMax, Is.EqualTo(new Vector2(0.4f, 0.7f)));
            Assert.That(face.rectTransform.anchoredPosition, Is.EqualTo(new Vector2(40f, -60f)));
            Assert.That(face.rectTransform.sizeDelta, Is.EqualTo(new Vector2(500f, 600f)));
            Assert.That(
                Quaternion.Angle(face.rectTransform.localRotation, Quaternion.Euler(0f, 0f, -18f)),
                Is.LessThan(0.01f));

            float timeout = Time.realtimeSinceStartup + 20f;
            while (BlobsterSceneTransition.IsRunning && Time.realtimeSinceStartup < timeout)
                yield return null;

            Assert.That(BlobsterSceneTransition.IsRunning, Is.False);
            Object.Destroy(eyesSprite);
            Object.Destroy(fastStyle);
        }

        private static BlobsterTransitionStyle CreateFastStyle(BlobsterTransitionStyle source)
        {
            BlobsterTransitionStyle style = Object.Instantiate(source);
            JsonUtility.FromJsonOverwrite(
                "{\"presentation\":{\"closeDuration\":0.05,\"coveredHoldDuration\":0.01,\"openDuration\":0.05,\"mouthReshapeDuration\":0.02}}",
                style);
            return style;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "Missing field: " + fieldName);
            field.SetValue(target, value);
        }
    }
}
