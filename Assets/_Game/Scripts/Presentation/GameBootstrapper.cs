using System;
using Blobs.Application;
using Blobs.Core;
using Blobs.Content;
using Blobs.Input;
using UnityEngine;
using Blobs.Debugging;

namespace Blobs.Presentation
{
    /// <summary>
    /// Scene composition root for production gameplay. It creates the Application session and wires
    /// Presentation/Input collaborators, but it must not apply effects or own gameplay flow.
    /// </summary>
    public sealed class GameBootstrapper : MonoBehaviour, IGameplaySessionHost, IGameplayPresentationStatus
    {
        [SerializeField] private BoardPresenter boardPresenter;

        [SerializeField] private LevelDefinitionAsset levelAsset;
        [SerializeField] private LevelDefinitionAsset[] levels;

        [SerializeField] private CameraPresenter cameraPresenter;
        [SerializeField] private GameplayInputAdapter inputAdapter;
        [SerializeField] private GameplayCommandAdapter commandAdapter;
        [SerializeField] private GameplayFeedbackPresenter feedbackPresenter;
        [SerializeField, Min(0)] private int initialLevelIndex;

        private GameSession _session;
        private QueuedGameplayCommands _commands;
        private GameSessionSnapshot _presentedSnapshot;

        public int CurrentLevelIndex { get; private set; }

        /// <inheritdoc />
        public event Action<IGameplayCommands, IGameplayState> SessionStarted;

        /// <inheritdoc />
        public event Action<GameSessionSnapshot> PresentationSettled;

        /// <inheritdoc />
        public IGameplayCommands CurrentCommands => _commands;

        /// <inheritdoc />
        public IGameplayState CurrentState => _session;

        /// <inheritdoc />
        public GameSessionSnapshot PresentedSnapshot => _presentedSnapshot;

        private void Start()
        {

            CurrentLevelIndex = ClampLevelIndex(initialLevelIndex);

            if (levels != null && levels.Length > 0)
                LoadLevel(CurrentLevelIndex);
        }




        public void LoadLevel(int index)
        {
            if (levels == null || levels.Length == 0)
                return;

            CurrentLevelIndex = ClampLevelIndex(index);
            StartLevel(levels[CurrentLevelIndex]);
        }

        public void PreviousLevel()
        {
            if (levels == null || levels.Length == 0)
                return;

            LoadLevel((CurrentLevelIndex - 1 + levels.Length) % levels.Length);
        }

        private int ClampLevelIndex(int index)
        {
            return levels == null || levels.Length == 0
                ? 0
                : Mathf.Clamp(index, 0, levels.Length - 1);
        }

        public void ExitGame()
        {
            UnityEngine.Application.Quit();
        }

        public void NextLevel()
        {
            if (levels == null || levels.Length == 0)
                return;

            LoadLevel((CurrentLevelIndex + 1) % levels.Length);
        }

        /// <summary>
        /// Starts a level from authored content by converting it to Core data and wiring scene collaborators.
        /// </summary>
        public void StartLevel(LevelDefinitionAsset asset)
        {
            if (asset == null)
            {
                Debug.LogError("GameBootstrapper cannot start without a level asset.", this);
                return;
            }

            if (!HasRequiredSceneCollaborators())
                return;

            LevelDefinition level = LevelAssetMapper.ToCore(asset);

            new Debugger(message => Debug.Log(message));
            _session = new GameSession(level, new MoveResolver());

            boardPresenter.SnapshotChanged -= HandlePresentationSettled;
            boardPresenter.SnapshotChanged += HandlePresentationSettled;
            boardPresenter.Initialize(
                _session,
                asset.Palette,
                level: asset);
            _commands = new QueuedGameplayCommands(_session, boardPresenter);

            inputAdapter.Initialize(_commands, boardPresenter.CellSize, boardPresenter.Origin);
            commandAdapter.Initialize(_commands);
            if (feedbackPresenter != null)
                feedbackPresenter.Initialize(inputAdapter, _session);
            if (cameraPresenter != null)
                cameraPresenter.FitCameraToBoard(_session.CurrentState.Width, _session.CurrentState.Height, boardPresenter.CellSize);

            SessionStarted?.Invoke(_commands, _session);
        }

        private void HandlePresentationSettled(GameSessionSnapshot snapshot)
        {
            _presentedSnapshot = snapshot;
            PresentationSettled?.Invoke(snapshot);
        }

        private void OnDestroy()
        {
            if (boardPresenter != null)
                boardPresenter.SnapshotChanged -= HandlePresentationSettled;
        }

        /// <summary>
        /// Validates required authored references before creating a gameplay session. Silently
        /// adding replacements here would bypass prefab catalogs, input configuration, and roots.
        /// </summary>
        private bool HasRequiredSceneCollaborators()
        {
            if (boardPresenter != null &&
                inputAdapter != null &&
                commandAdapter != null)
                return true;

            Debug.LogError(
                "GameBootstrapper requires authored BoardPresenter, " +
                "GameplayInputAdapter, and GameplayCommandAdapter references.",
                this);
            return false;
        }
    }
}
