using Blobs.Application;
using Blobs.Core;
using UnityEngine;
using Blobs.Content;
using Blobs.Input;

namespace Blobs.Presentation
{
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
