using System;
using Blobs.Application;
using Blobs.Core;
using Blobs.Input;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Blobs.Tests.EditMode
{
    public sealed class GameplayDragInputTests
    {
        private GameObject _root;
        private Camera _camera;
        private GameplayInputAdapter _adapter;
        private GameSession _session;
        private Mouse _mouse;
        private Touchscreen _touchscreen;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("Drag input test");
            _camera = _root.AddComponent<Camera>();
            _camera.orthographic = true;
            _camera.orthographicSize = 2f;
            _camera.pixelRect = new Rect(0, 0, 400, 400);
            _camera.transform.position = new Vector3(1, 0, -10);
            _adapter = _root.AddComponent<GameplayInputAdapter>();
            var serialized = new SerializedObject(_adapter);
            serialized.FindProperty("boardCamera").objectReferenceValue = _camera;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            _session = new GameSession(new LevelDefinition("drag-test", LevelDefinition.CurrentSchemaVersion, 4, 1,
                new BlobDefinition[]
                {
                    new NormalBlobDefinition("source", new GridPosition(0, 0), BlobColor.Red),
                    new NormalBlobDefinition("target", new GridPosition(1, 0), BlobColor.Blue),
                    new FlagBlobDefinition("flag", new GridPosition(2, 0), BlobColor.Red)
                }, Array.Empty<TileDefinition>()));
            _mouse = InputSystem.AddDevice<Mouse>();
            _touchscreen = InputSystem.AddDevice<Touchscreen>();
            _adapter.Initialize(_session, 1f);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_root);
            InputSystem.RemoveDevice(_mouse);
            InputSystem.RemoveDevice(_touchscreen);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MouseAndTouchCommitOnReleaseAtTarget(bool touch)
        {
            Send(touch, 0, true);
            Assert.That(_session.SelectedBlobId, Is.EqualTo("source"));
            Assert.That(_session.MoveCount, Is.Zero);
            Send(touch, 1, false);
            Assert.That(_session.MoveCount, Is.EqualTo(1));
            Assert.That(_session.CurrentState.GetBlob("target"), Is.Null);
        }

        [TestCase(0, 3)]
        [TestCase(3, 1)]
        [TestCase(0, 0)]
        public void InvalidEndpointsDoNotCommit(int source, int target)
        {
            Send(false, source, true);
            Send(false, target, false);
            Assert.That(_session.MoveCount, Is.Zero);
            Assert.That(_session.SelectedBlobId, Is.Null);
        }

        [Test]
        public void DisablingAdapterCancelsPendingDrag()
        {
            Send(false, 0, true);
            _adapter.enabled = false;
            Send(false, 1, false);
            Assert.That(_session.MoveCount, Is.Zero);
            Assert.That(_session.SelectedBlobId, Is.Null);
        }

        [Test]
        public void CanceledTouchDoesNotCommit()
        {
            Send(true, 0, true);
            InputSystem.QueueStateEvent(_touchscreen, new TouchState
            {
                touchId = 1,
                position = _camera.WorldToScreenPoint(Vector3.right),
                phase = UnityEngine.InputSystem.TouchPhase.Canceled
            });
            InputSystem.Update();
            Assert.That(_session.MoveCount, Is.Zero);
            Assert.That(_session.SelectedBlobId, Is.Null);
        }

        private void Send(bool touch, int x, bool pressed)
        {
            Vector2 position = _camera.WorldToScreenPoint(new Vector3(x, 0, 0));
            if (touch)
                InputSystem.QueueStateEvent(_touchscreen, new TouchState
                {
                    touchId = 1, position = position,
                    phase = pressed ? UnityEngine.InputSystem.TouchPhase.Began : UnityEngine.InputSystem.TouchPhase.Ended
                });
            else
                InputSystem.QueueStateEvent(_mouse, new MouseState { position = position }.WithButton(MouseButton.Left, pressed));
            InputSystem.Update();
        }
    }
}
