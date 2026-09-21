using Blobs.Application;
using TMPro;
using UnityEngine;

namespace Blobs.UI
{
    /// <summary>
    /// Production HUD presenter for gameplay state. It observes Application state and forwards UI
    /// button actions to Application commands without depending on Presentation or Core internals.
    /// </summary>
    public sealed class GameplayHudPresenter : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour sessionHostBehaviour;
        [SerializeField] private TMP_Text moveCountText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private HudButtonView restartButton;
        [SerializeField] private HudButtonView undoButton;
        [SerializeField] private HudButtonView pauseButton;
        [SerializeField] private HudButtonView resumeButton;
        [SerializeField] private GameObject completionRoot;
        [SerializeField] private string playingStatus = "";
        [SerializeField] private string completeStatus = "Complete!";

        private IGameplaySessionHost _sessionHost;
        private IGameplayPresentationStatus _presentationStatus;

        private IGameplayCommands _commands;
        private IGameplayState _state;
        private bool _completionPresentationReady;

        private void Awake()
        {
            if (restartButton != null)
                restartButton.Button.onClick.AddListener(Restart);

            if (undoButton != null)
                undoButton.Button.onClick.AddListener(Undo);

            if (pauseButton != null)
                pauseButton.Button.onClick.AddListener(PauseGame);

            if (resumeButton != null)
                resumeButton.Button.onClick.AddListener(ResumeGame);

            BindToHost();
        }

        private void OnEnable()
        {
            BindToHost();
        }

        private void OnDestroy()
        {
            if (restartButton != null)
                restartButton.Button.onClick.RemoveListener(Restart);

            if (undoButton != null)
                undoButton.Button.onClick.RemoveListener(Undo);

            if (pauseButton != null)
                pauseButton.Button.onClick.RemoveListener(PauseGame);

            if (resumeButton != null)
                resumeButton.Button.onClick.RemoveListener(ResumeGame);

            UnbindState();

            if (_sessionHost != null)
                _sessionHost.SessionStarted -= BindSession;

            UnbindPresentationStatus();
        }

        /// <summary>
        /// Binds this HUD to an explicit session host. Tests and custom scene setup can call this directly.
        /// </summary>
        public void Initialize(IGameplaySessionHost sessionHost)
        {
            if (_sessionHost != null)
                _sessionHost.SessionStarted -= BindSession;

            UnbindState();
            UnbindPresentationStatus();

            _sessionHost = sessionHost;

            if (_sessionHost == null)
                return;

            _sessionHost.SessionStarted += BindSession;
            _presentationStatus = _sessionHost as IGameplayPresentationStatus;
            if (_presentationStatus != null)
                _presentationStatus.PresentationSettled += HandlePresentationSettled;

            if (_sessionHost.CurrentCommands != null && _sessionHost.CurrentState != null)
                BindSession(_sessionHost.CurrentCommands, _sessionHost.CurrentState);
        }

        private void BindToHost()
        {
            if (_sessionHost != null)
                return;

            if (sessionHostBehaviour is IGameplaySessionHost assignedSessionHost)
            {
                Initialize(assignedSessionHost);
                return;
            }

            foreach (var behaviour in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (behaviour is IGameplaySessionHost sessionHost)
                {
                    Initialize(sessionHost);
                    return;
                }
            }
        }

        private void BindSession(IGameplayCommands commands, IGameplayState state)
        {
            UnbindState();

            _commands = commands;
            _state = state;
            _completionPresentationReady =
                IsPresentedCompletion(_presentationStatus?.PresentedSnapshot);

            if (_state != null)
            {
                _state.SnapshotChanged += Render;
                Render(_state.CreateSnapshot());
            }
            else
            {
                Render(null);
            }
        }

        private void Restart()
        {
            _commands?.Restart();
        }

        private void Undo()
        {
            _commands?.Undo();
        }

        private void PauseGame()
        {
            _sessionHost?.PauseGame();
        }

        private void ResumeGame()
        {
            _sessionHost?.ResumeGame();
        }

        private void Render(GameSessionSnapshot snapshot)
        {
            if (moveCountText != null)
                moveCountText.text = snapshot == null ? "0" : snapshot.MoveCount.ToString();

            bool isComplete = snapshot != null && snapshot.IsComplete;
            if (!isComplete)
                _completionPresentationReady = false;

            bool showCompletion = isComplete &&
                (_presentationStatus == null || _completionPresentationReady);

            if (statusText != null)
                statusText.text = showCompletion ? completeStatus : playingStatus;

            if (completionRoot != null)
                completionRoot.SetActive(showCompletion);


            if (undoButton == null)
                return;

            if (snapshot == null || isComplete || !snapshot.CanUndo)
            {
                undoButton.Disable();
                return;
            }

            undoButton.Enable();
        }

        private void HandlePresentationSettled(GameSessionSnapshot presentedSnapshot)
        {
            if (_state == null)
                return;

            GameSessionSnapshot current = _state.CreateSnapshot();
            _completionPresentationReady =
                current != null &&
                current.IsComplete &&
                IsSameCommittedState(current, presentedSnapshot);
            Render(current);
        }

        private bool IsPresentedCompletion(GameSessionSnapshot presentedSnapshot)
        {
            if (_state == null)
                return false;

            GameSessionSnapshot current = _state.CreateSnapshot();
            return current != null &&
                current.IsComplete &&
                IsSameCommittedState(current, presentedSnapshot);
        }

        private static bool IsSameCommittedState(
            GameSessionSnapshot current,
            GameSessionSnapshot presented)
        {
            return presented != null &&
                presented.IsComplete &&
                current.LevelId == presented.LevelId &&
                current.MoveCount == presented.MoveCount;
        }

        private void UnbindPresentationStatus()
        {
            if (_presentationStatus != null)
                _presentationStatus.PresentationSettled -= HandlePresentationSettled;

            _presentationStatus = null;
            _completionPresentationReady = false;
        }

        private void UnbindState()
        {
            if (_state != null)
            {
                _state.SnapshotChanged -= Render;
            }

            _commands = null;
            _state = null;
        }
    }
}
