using System;
using System.Collections.Generic;
using System.Reflection;
using Blobs.Application;
using Blobs.Core;
using Blobs.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Blobs.Tests.EditMode
{
    public sealed class GameplayHudPresenterTests
    {
        private readonly List<UnityEngine.Object> _createdObjects = new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            for (int i = _createdObjects.Count - 1; i >= 0; i--)
            {
                if (_createdObjects[i] != null)
                    UnityEngine.Object.DestroyImmediate(_createdObjects[i]);
            }

            _createdObjects.Clear();
        }

        [Test]
        public void HudBindsToSessionHostUpdatesMoveCountAndForwardsRestart()
        {
            GameObject hud = CreateGameObject("HUD");
            hud.SetActive(false);
            var presenter = hud.AddComponent<GameplayHudPresenter>();
            var moveText = CreateGameObject("Move Text").AddComponent<TextMeshProUGUI>();
            var restartButton = CreateGameObject("Restart Button").AddComponent<Button>();
            var completionRoot = CreateGameObject("Completion Root");
            var commands = new FakeCommands();
            var state = new FakeState();
            var host = new FakeSessionHost(commands, state);

            SetPrivateField(presenter, "moveCountText", moveText);
            SetPrivateField(presenter, "restartButton", restartButton);
            SetPrivateField(presenter, "completionRoot", completionRoot);

            hud.SetActive(true);
            presenter.Initialize(host);

            Assert.That(moveText.text, Is.EqualTo("0"));
            Assert.That(completionRoot.activeSelf, Is.False);

            state.MoveCount = 2;
            state.RaiseSnapshotChanged();

            Assert.That(moveText.text, Is.EqualTo("2"));

            restartButton.onClick.Invoke();

            Assert.That(commands.RestartCount, Is.EqualTo(1));
        }

        private GameObject CreateGameObject(string name)
        {
            var gameObject = new GameObject(name);
            _createdObjects.Add(gameObject);
            return gameObject;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "Missing field: " + fieldName);
            field.SetValue(target, value);
        }

        private sealed class FakeCommands : IGameplayCommands
        {
            public int RestartCount { get; private set; }

            public BlobSelectionResult SelectBlobAt(GridPosition position)
            {
                return BlobSelectionResult.Cleared();
            }

            public void Restart()
            {
                RestartCount++;
            }
        }

        private sealed class FakeState : IGameplayState
        {
            public event Action<MoveResult> MoveResolved;
            public event Action<GameSessionSnapshot> SnapshotChanged;
            public event Action<GameSessionSnapshot> StateRestored;
            public event Action<BlobSelectionResult> BlobSelected;

            public int MoveCount { get; set; }

            public GameSessionSnapshot CreateSnapshot()
            {
                return new GameSessionSnapshot(
                    "hud-test",
                    Array.Empty<BlobState>(),
                    Array.Empty<TileState>(),
                    MoveCount,
                    false);
            }

            public void RaiseSnapshotChanged()
            {
                MoveResolved?.Invoke(MoveResult.Failed(null, null, MoveFailureReason.None));
                SnapshotChanged?.Invoke(CreateSnapshot());
            }

            public void RaiseStateRestored()
            {
                StateRestored?.Invoke(CreateSnapshot());
            }
        }

        private sealed class FakeSessionHost : IGameplaySessionHost
        {
            public FakeSessionHost(IGameplayCommands commands, IGameplayState state)
            {
                CurrentCommands = commands;
                CurrentState = state;
            }

            public event Action<IGameplayCommands, IGameplayState> SessionStarted;

            public IGameplayCommands CurrentCommands { get; }
            public IGameplayState CurrentState { get; }

            public void RaiseSessionStarted()
            {
                SessionStarted?.Invoke(CurrentCommands, CurrentState);
            }
        }
    }
}
