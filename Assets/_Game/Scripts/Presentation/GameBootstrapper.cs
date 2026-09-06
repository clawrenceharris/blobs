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
    public sealed class GameBootstrapper : MonoBehaviour, IGameplaySessionHost
    {
        [SerializeField] private BoardPresenter boardPresenter;

        [SerializeField] private LevelDefinitionAsset levelAsset;
        [SerializeField] private BackgroundView backgroundView;
        [SerializeField] private CameraPresenter cameraPresenter;
        [SerializeField] private GameplayInputAdapter inputAdapter;
        [SerializeField] private GameplayCommandAdapter commandAdapter;
        [SerializeField] private GameplayFeedbackPresenter feedbackPresenter;

        private GameSession _session;

        /// <inheritdoc />
        public event Action<IGameplayCommands, IGameplayState> SessionStarted;

        /// <inheritdoc />
        public IGameplayCommands CurrentCommands => _session;

        /// <inheritdoc />
        public IGameplayState CurrentState => _session;

        private void Start()
        {
            StartLevel(levelAsset);
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
            // if (backgroundView != null)
            // {
            //     backgroundView.gameObject.SetActive(true);
            //     backgroundView.Apply(asset.Palette);
            // }
            new Debugger(message => Debug.Log(message));
            _session = new GameSession(level, new MoveResolver());

            boardPresenter.Initialize(
                _session,
                asset.Palette);
            inputAdapter.Initialize(_session, boardPresenter.CellSize);
            commandAdapter.Initialize(_session);
            if (feedbackPresenter != null)
                feedbackPresenter.Initialize(inputAdapter);
            if (cameraPresenter != null)
                cameraPresenter.FitCameraToBoard(_session.CurrentState, boardPresenter.CellSize);

            SessionStarted?.Invoke(_session, _session);
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
