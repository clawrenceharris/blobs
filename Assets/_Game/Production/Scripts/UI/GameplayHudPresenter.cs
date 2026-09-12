using Blobs.Application;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        [SerializeField] private Button restartButton;
        [SerializeField] private Button undoButton;
        [SerializeField] private GameObject completionRoot;
        [SerializeField] private string playingStatus = "";
        [SerializeField] private string completeStatus = "Complete!";

        private IGameplaySessionHost _sessionHost;

        private IGameplayCommands _commands;
        private IGameplayState _state;

        private void Awake()
        {
            if (restartButton != null)
                restartButton.onClick.AddListener(Restart);

            if (undoButton != null)
                undoButton.onClick.AddListener(Undo);

            BindToHost();
        }

        private void OnEnable()
        {
            BindToHost();
        }

        private void OnDestroy()
        {
            if (restartButton != null)
                restartButton.onClick.RemoveListener(Restart);

            if (undoButton != null)
                undoButton.onClick.RemoveListener(Undo);

            UnbindState();

            if (_sessionHost != null)
                _sessionHost.SessionStarted -= BindSession;
        }

        /// <summary>
        /// Binds this HUD to an explicit session host. Tests and custom scene setup can call this directly.
        /// </summary>
        public void Initialize(IGameplaySessionHost sessionHost)
        {
            if (_sessionHost != null)
                _sessionHost.SessionStarted -= BindSession;

            _sessionHost = sessionHost;

            if (_sessionHost == null)
                return;

            _sessionHost.SessionStarted += BindSession;

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

        private void Render(GameSessionSnapshot snapshot)
        {
            if (moveCountText != null)
                moveCountText.text = snapshot == null ? "0" : snapshot.MoveCount.ToString();

            bool isComplete = snapshot != null && snapshot.IsComplete;

            if (statusText != null)
                statusText.text = isComplete ? completeStatus : playingStatus;

            if (completionRoot != null)
                completionRoot.SetActive(isComplete);
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
