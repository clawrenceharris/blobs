using System;
using System.Collections;
using UnityEngine.TestTools;
using System.Collections.Generic;
using System.Reflection;
using Blobs.Application;
using Blobs.Core;
using Blobs.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Blobs.Tests.PlayMode
{
    public sealed class GameplayHudPresenterTests
    {
        private readonly List<UnityEngine.Object> _createdObjects = new List<UnityEngine.Object>();

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            for (int i = _createdObjects.Count - 1; i >= 0; i--)
            {
                if (_createdObjects[i] != null)
                    UnityEngine.Object.Destroy(_createdObjects[i]);
            }

            _createdObjects.Clear();
            yield return null;
        }

        [UnityTest]
        public IEnumerator HudBindsToSessionHostUpdatesMoveCountAndForwardsRestart()
        {
            GameObject hud = CreateGameObject("HUD");
            hud.SetActive(false);
            var presenter = hud.AddComponent<GameplayHudPresenter>();
            var moveText = CreateGameObject("Move Text").AddComponent<TextMeshProUGUI>();
            HudButtonView restartButton = CreateHudButton("Restart Button");
            var completionRoot = CreateGameObject("Completion Root");
            var commands = new FakeCommands();
            var state = new FakeState();
            var host = new FakeSessionHost(commands, state);

            SetPrivateField(presenter, "moveCountText", moveText);
            SetPrivateField(presenter, "restartButton", restartButton);
            SetPrivateField(presenter, "completionRoot", completionRoot);

            hud.SetActive(true);
            presenter.Initialize(host);
            yield return null;

            Assert.That(moveText.text, Is.EqualTo("0"));
            Assert.That(completionRoot.activeSelf, Is.False);

            state.MoveCount = 2;
            state.RaiseSnapshotChanged();

            Assert.That(moveText.text, Is.EqualTo("2"));

            state.IsComplete = true;
            state.RaiseSnapshotChanged();
            Assert.That(completionRoot.activeSelf, Is.True);

            restartButton.Button.onClick.Invoke();

            Assert.That(commands.RestartCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator WinningSnapshotWaitsForPresentationBeforeShowingCompletion()
        {
            GameObject hud = CreateGameObject("Winning HUD");
            hud.SetActive(false);
            var presenter = hud.AddComponent<GameplayHudPresenter>();
            var statusText = CreateGameObject("Status Text").AddComponent<TextMeshProUGUI>();
            var completionRoot = CreateGameObject("Completion Root");
            var state = new FakeState();
            var host = new FakePresentationSessionHost(new FakeCommands(), state);

            SetPrivateField(presenter, "statusText", statusText);
            SetPrivateField(presenter, "completionRoot", completionRoot);

            hud.SetActive(true);
            presenter.Initialize(host);
            yield return null;

            state.MoveCount = 1;
            state.IsComplete = true;
            state.RaiseSnapshotChanged();

            Assert.That(completionRoot.activeSelf, Is.False);
            Assert.That(statusText.text, Is.Empty);

            host.RaisePresentationSettled(state.CreateSnapshot());

            Assert.That(completionRoot.activeSelf, Is.True);
            Assert.That(statusText.text, Is.EqualTo("Complete!"));
        }

        [UnityTest]
        public IEnumerator RestartHidesVictoryAndIgnoresStaleWinningPresentation()
        {
            GameObject hud = CreateGameObject("Restart HUD");
            hud.SetActive(false);
            var presenter = hud.AddComponent<GameplayHudPresenter>();
            var completionRoot = CreateGameObject("Completion Root");
            var state = new FakeState { MoveCount = 2, IsComplete = true };
            var host = new FakePresentationSessionHost(new FakeCommands(), state);
            GameSessionSnapshot winningSnapshot = state.CreateSnapshot();

            SetPrivateField(presenter, "completionRoot", completionRoot);

            hud.SetActive(true);
            presenter.Initialize(host);
            host.RaisePresentationSettled(winningSnapshot);
            Assert.That(completionRoot.activeSelf, Is.True);

            state.MoveCount = 0;
            state.IsComplete = false;
            state.RaiseSnapshotChanged();
            host.RaisePresentationSettled(winningSnapshot);
            yield return null;

            Assert.That(completionRoot.activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator UndoAvailabilityUsesImmediateLogicalSnapshotDuringPlayback()
        {
            GameObject hud = CreateGameObject("Undo HUD");
            hud.SetActive(false);
            var presenter = hud.AddComponent<GameplayHudPresenter>();
            HudButtonView undoButton = CreateHudButton("Undo Button");
            var state = new FakeState();
            var host = new FakePresentationSessionHost(new FakeCommands(), state);

            SetPrivateField(presenter, "undoButton", undoButton);

            hud.SetActive(true);
            presenter.Initialize(host);
            yield return null;
            Assert.That(undoButton.Button.interactable, Is.False);

            state.MoveCount = 1;
            state.UndoAvailable = true;
            state.RaiseSnapshotChanged();

            Assert.That(undoButton.Button.interactable, Is.True);
        }

        private GameObject CreateGameObject(string name)
        {
            var gameObject = new GameObject(name);
            _createdObjects.Add(gameObject);
            return gameObject;
        }

        private HudButtonView CreateHudButton(string name)
        {
            GameObject buttonObject = CreateGameObject(name);
            var button = buttonObject.AddComponent<Button>();
            var icon = buttonObject.AddComponent<Image>();
            var view = buttonObject.AddComponent<HudButtonView>();
            SetPrivateField(view, "button", button);
            SetPrivateField(view, "icon", icon);
            return view;
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

            public BlobSelectionResult BeginDragAt(GridPosition position) => SelectBlobAt(position);
            public BlobSelectionResult EndDragAt(GridPosition position) => SelectBlobAt(position);
            public void CancelDrag() { }

            public void Restart()
            {
                RestartCount++;
            }

            public bool Undo()
            {
                return false;
            }
        }

        private sealed class FakeState : IGameplayState
        {
            public event Action<MoveResult> MoveResolved;
            public event Action<UndoResult> UndoResolved;
            public event Action<GameSessionSnapshot> SnapshotChanged;
            public event Action<GameSessionSnapshot> StateRestored;
            public event Action<BlobSelectionResult> BlobSelected;

            public int MoveCount { get; set; }
            public bool IsComplete { get; set; }
            public bool UndoAvailable { get; set; }
            public bool CanUndo => UndoAvailable;

            public GameSessionSnapshot CreateSnapshot()
            {
                return new GameSessionSnapshot(
                    "hud-test",
                    new BoardState(2, 1, new List<BlobState>(), new List<TileState>()),
                    MoveCount,
                    IsComplete,
                    CanUndo);
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

        private sealed class FakePresentationSessionHost :
            IGameplaySessionHost,
            IGameplayPresentationStatus
        {
            public FakePresentationSessionHost(
                IGameplayCommands commands,
                IGameplayState state)
            {
                CurrentCommands = commands;
                CurrentState = state;
            }

            public event Action<IGameplayCommands, IGameplayState> SessionStarted;
            public event Action<GameSessionSnapshot> PresentationSettled;

            public IGameplayCommands CurrentCommands { get; }
            public IGameplayState CurrentState { get; }
            public GameSessionSnapshot PresentedSnapshot { get; private set; }

            public void RaisePresentationSettled(GameSessionSnapshot snapshot)
            {
                PresentedSnapshot = snapshot;
                PresentationSettled?.Invoke(snapshot);
            }

            public void PauseGame()
            {
            }

            public void ResumeGame()
            {
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
            public void PauseGame()
            {
            }
            public void ResumeGame()
            {
            }
        }
    }
}
