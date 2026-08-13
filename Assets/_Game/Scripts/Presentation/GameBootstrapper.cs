using System;
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
   
   
        [SerializeField] private bool startSampleLevelOnStart = true;
        [SerializeField] private LevelDefinitionAsset levelAsset;
        [SerializeField] private BackgroundView backgroundView;
        [SerializeField] private CameraPresenter cameraPresenter;
        [SerializeField] private GameplayInputAdapter inputAdapter;

        private GameSession _session;
        private string _selectedBlobId;

        public event Action<GameSessionSnapshot> SnapshotChanged;
        public event Action<MoveResult> MoveResolved;


        public GameSessionSnapshot CurrentSnapshot => _session?.CreateSnapshot();

        private void Start()
        {
           StartLevel(levelAsset);
        }

       
         public void StartLevel(LevelDefinitionAsset asset)
        {
            
            _selectedBlobId = null;

            LevelDefinition level = LevelAssetMapper.ToCore(asset);
            if (backgroundView != null)
            {
                backgroundView.gameObject.SetActive(true);
                backgroundView.Apply(asset.VisualTheme);
            }
            _session = new GameSession(level, new MoveResolver());
            EnsureBoardPresenter();

            boardPresenter.Initialize(asset.VisualTheme);
            cameraPresenter.FitCameraToBoard(_session.CurrentState, boardPresenter.CellSize);
           
            RenderSnapshot();
        }

        public void Undo()
        {
            if (_session == null || !_session.Undo())
                return;

            _selectedBlobId = null;
            RenderSnapshot();
        }

        public void Restart()
        {
            if (_session == null)
                StartLevel(levelAsset);
            else
                _session.Restart();

            _selectedBlobId = null;
            RenderSnapshot();
        }

        private void HandleBlobSelected(string blobId)
        {
            if (_session == null)
                StartLevel(levelAsset);

            if (string.IsNullOrEmpty(_selectedBlobId))
            {
                _selectedBlobId = blobId;
                return;
            }

            if (_selectedBlobId == blobId)
            {
                _selectedBlobId = null;
                return;
            }

            ExecuteMove(_selectedBlobId, blobId);
        }

        private void ExecuteMove(string sourceId, string targetId)
        {
            var result = _session.ExecuteMove(new MoveIntent(sourceId, targetId));
            _selectedBlobId = null;
            MoveResolved?.Invoke(result);

            if (result.Succeeded)
                RenderSnapshot();
        }

        private void RenderSnapshot()
        {
            var snapshot = _session.CreateSnapshot();
            EnsureBoardPresenter();
            boardPresenter.Rebuild(snapshot);
            SnapshotChanged?.Invoke(snapshot);
        }

        private void EnsureBoardPresenter()
        {
            if (boardPresenter == null)
                boardPresenter = GetComponentInChildren<BoardPresenter>();
            if (boardPresenter == null)
                boardPresenter = gameObject.AddComponent<BoardPresenter>();

            boardPresenter.BlobSelected -= HandleBlobSelected;
            boardPresenter.BlobSelected += HandleBlobSelected;
        }
    }
}
