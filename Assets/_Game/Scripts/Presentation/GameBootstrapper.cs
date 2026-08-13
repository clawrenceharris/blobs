using System;
using System.Collections.Generic;
using Blobs.Application;
using Blobs.Core;
using UnityEngine;
using Blobs.Content;

namespace Blobs.Presentation
{
    public sealed class GameBootstrapper : MonoBehaviour
    {
        [SerializeField] private BoardPresenter boardPresenter; 
   
   
        [SerializeField] private bool startSampleLevelOnStart = true;
        [SerializeField] private LevelDefinitionAsset levelAsset;
        [SerializeField] private BackgroundView backgroundView;
        [SerializeField] private CameraPresenter cameraPresenter;

        private GameSession _session;
        private string _selectedBlobId;

        public event Action<GameSessionSnapshot> SnapshotChanged;
        public event Action<MoveResult> MoveResolved;


        public GameSessionSnapshot CurrentSnapshot => _session?.CreateSnapshot();

        private void Start()
        {
           if (startSampleLevelOnStart)
               StartLevel(levelAsset);
        }

       
         public void StartLevel(LevelDefinitionAsset asset)
        {
            if (asset == null)
            {
                Debug.LogError("GameBootstrapper cannot start without a level asset.", this);
                return;
            }

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
            if (cameraPresenter != null)
                cameraPresenter.FitCameraToBoard(_session.CurrentState, boardPresenter.CellSize);
           
            RenderSnapshot();
        }

        public void Undo()
        {
            if (_session == null)
                return;

            var result = _session.UndoLastMove();
            if (!result.Succeeded)
                return;

            _selectedBlobId = null;
            ApplyEffects(result.Effects);
        }

        public void Restart()
        {
            if (_session == null)
            {
                StartLevel(levelAsset);
                return;
            }
            else
                _session.Restart();

            _selectedBlobId = null;
            RenderSnapshot();
        }

        private void HandleBlobSelected(string blobId)
        {
            if (_session == null)
                StartLevel(levelAsset);
            if (_session == null)
                return;

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
                ApplyEffects(result.Effects);
        }

        private void ApplyEffects(IReadOnlyList<IBoardEffect> effects)
        {
            var snapshot = _session.CreateSnapshot();
            EnsureBoardPresenter();
            boardPresenter.ApplyEffects(effects, snapshot);
            SnapshotChanged?.Invoke(snapshot);
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
