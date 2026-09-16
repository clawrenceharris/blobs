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
