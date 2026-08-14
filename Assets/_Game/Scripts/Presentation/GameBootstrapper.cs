using System;
using System.Collections.Generic;
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

            LevelDefinition level = LevelAssetMapper.ToCore(asset);
            if (backgroundView != null)
            {
                backgroundView.gameObject.SetActive(true);
                backgroundView.Apply(asset.VisualTheme);
            }
            _session = new GameSession(level, new MoveResolver());
            EnsureBoardPresenter();
            EnsureInputAdapter();

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

            RenderSnapshot();
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

            // boardPresenter.BlobSelected -= HandleBlobSelected;
            // boardPresenter.BlobSelected += HandleBlobSelected;
        }

        private void EnsureInputAdapter()
        {
            if (inputAdapter == null)
                inputAdapter = GetComponentInChildren<GameplayInputAdapter>();

            if (inputAdapter == null)
                return;

            // inputAdapter.BlobSelectionResolved -= HandleBlobSelectionResult;
            // inputAdapter.BlobSelectionResolved += HandleBlobSelectionResult;
            inputAdapter.Initialize(_session, boardPresenter.CellSize);
        }
    }
}
