using Blobs.Application;
using Blobs.Core;
using UnityEngine;
using Blobs.Content;
using Blobs.Input;

namespace Blobs.Presentation
{
    /// <summary>
    /// Scene composition root for production gameplay. It creates the Application session and wires
    /// Presentation/Input collaborators, but it must not apply effects or own gameplay flow.
    /// </summary>
    public sealed class GameBootstrapper : MonoBehaviour
    {
        [SerializeField] private BoardPresenter boardPresenter;

        [SerializeField] private LevelDefinitionAsset levelAsset;
        [SerializeField] private BackgroundView backgroundView;
        [SerializeField] private CameraPresenter cameraPresenter;
        [SerializeField] private GameplayInputAdapter inputAdapter;
        [SerializeField] private GameplayCommandAdapter commandAdapter;

        private GameSession _session;

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

            LevelDefinition level = LevelAssetMapper.ToCore(asset);
            if (backgroundView != null)
            {
                backgroundView.gameObject.SetActive(true);
                backgroundView.Apply(asset.VisualTheme);
            }
            _session = new GameSession(level, new MoveResolver());
            EnsureBoardPresenter();
            EnsureInputAdapter();
            EnsureCommandAdapter();

            boardPresenter.Initialize(_session, asset.VisualTheme);
            inputAdapter.Initialize(_session, boardPresenter.CellSize);
            commandAdapter.Initialize(_session);
            if (cameraPresenter != null)
                cameraPresenter.FitCameraToBoard(_session.CurrentState, boardPresenter.CellSize);

        }

        private void EnsureBoardPresenter()
        {
            if (boardPresenter == null)
                boardPresenter = GetComponentInChildren<BoardPresenter>();
            if (boardPresenter == null)
                boardPresenter = gameObject.AddComponent<BoardPresenter>();

        }
        private void EnsureCommandAdapter()
        {
            if (commandAdapter == null)
                commandAdapter = GetComponentInChildren<GameplayCommandAdapter>();
            if (commandAdapter == null)
                commandAdapter = gameObject.AddComponent<GameplayCommandAdapter>();

        }

        private void EnsureInputAdapter()
        {
            if (inputAdapter == null)
                inputAdapter = GetComponentInChildren<GameplayInputAdapter>();

            if (inputAdapter == null)
                inputAdapter = gameObject.AddComponent<GameplayInputAdapter>();
        }

    }
}
