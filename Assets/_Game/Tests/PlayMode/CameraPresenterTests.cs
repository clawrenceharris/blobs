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
    public sealed class CameraPresenterTests : PresentationTestFixture
    {
        [TestCase(0.5f)]
        [TestCase(2f)]
        public void BoardFitsBelowTopHud(float aspect)
        {
            Camera camera = CreateGameObject("HUD Framing Camera").AddComponent<Camera>();
            camera.orthographic = true;
            camera.aspect = aspect;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            CameraPresenter presenter = camera.gameObject.AddComponent<CameraPresenter>();
            SetPrivateField(presenter, "padding", 0.5f);
            SetPrivateField(presenter, "topHud", CreateTopHud());

            presenter.FitCameraToBoard(5, 7, 1f);

            Vector3 lowerLeft = camera.WorldToViewportPoint(new Vector3(-0.5f, -0.5f));
            Vector3 upperRight = camera.WorldToViewportPoint(new Vector3(4.5f, 6.5f));
            Vector3 center = camera.WorldToViewportPoint(new Vector3(2f, 3f));
            Assert.That(lowerLeft.x, Is.GreaterThanOrEqualTo(-0.001f));
            Assert.That(lowerLeft.y, Is.GreaterThanOrEqualTo(-0.001f));
            Assert.That(upperRight.x, Is.LessThanOrEqualTo(1.001f));
            Assert.That(upperRight.y, Is.LessThanOrEqualTo(0.751f));
            Assert.That(center.y, Is.EqualTo(0.375f).Within(0.001f));
            Assert.That(camera.transform.position.z, Is.EqualTo(-10f));
        }

        [UnityTest]
        public IEnumerator HiddenHudRestoresFullScreenFraming()
        {
            Camera camera = CreateGameObject("HUD Visibility Camera").AddComponent<Camera>();
            camera.orthographic = true;
            camera.aspect = 1f;
            CameraPresenter presenter = camera.gameObject.AddComponent<CameraPresenter>();
            RectTransform hud = CreateTopHud();
            SetPrivateField(presenter, "topHud", hud);
            presenter.FitCameraToBoard(5, 7, 1f);

            hud.gameObject.SetActive(false);
            yield return null;
            yield return null;

            Assert.That(camera.orthographicSize, Is.EqualTo(3f).Within(0.001f));
            Assert.That(camera.transform.position.y, Is.EqualTo(3f).Within(0.001f));
        }

        private RectTransform CreateTopHud()
        {
            Canvas canvas = CreateGameObject("HUD Canvas").AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            RectTransform hud = CreateGameObject("Top HUD").AddComponent<RectTransform>();
            hud.SetParent(canvas.transform, false);
            hud.anchorMin = new Vector2(0f, 0.75f);
            hud.anchorMax = Vector2.one;
            hud.offsetMin = Vector2.zero;
            hud.offsetMax = Vector2.zero;
            return hud;
        }

        [UnityTest]
        public IEnumerator CameraFramingUsesLiveAspectAndPreservesDepth()
        {
            GameObject cameraObject = CreateGameObject("Presentation Camera");
            cameraObject.transform.position = new Vector3(10f, 20f, -10f);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.aspect = 0.5f;
            CameraPresenter presenter = cameraObject.AddComponent<CameraPresenter>();
            SetPrivateField(presenter, "padding", 0.5f);

            presenter.FitCameraToBoard(width: 5, height: 3, cellSize: 1.25f);
            yield return null;

            Assert.That(cameraObject.transform.position.x, Is.EqualTo(2.5f).Within(0.001f));
            Assert.That(cameraObject.transform.position.y, Is.EqualTo(1.25f).Within(0.001f));
            Assert.That(cameraObject.transform.position.z, Is.EqualTo(-10f).Within(0.001f));
            Assert.That(camera.orthographicSize, Is.EqualTo(6f).Within(0.001f));
        }
    }
}
