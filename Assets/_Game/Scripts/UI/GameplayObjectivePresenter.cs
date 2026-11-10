using System.Collections.Generic;
using Blobs.Application;
using Blobs.Content;
using Blobs.Core;
using UnityEngine;

namespace Blobs.UI
{
    /// <summary>
    /// Builds one HUD counter per objective item and keeps the remaining amounts in sync with the
    /// session. When the host reports presentation timing, counts change once the board has
    /// finished animating so a counter drops as the monster is eaten or nested on screen.
    /// </summary>
    public sealed class GameplayObjectivePresenter : MonoBehaviour
    {
        [SerializeField] private ObjectiveViewCatalogAsset viewCatalog;
        [SerializeField] private MonoBehaviour sessionHostBehaviour;
        [SerializeField, Tooltip("Parent for counter views. Defaults to this transform.")]
        private Transform viewRoot;
        [SerializeField, Tooltip("Used when the session host does not supply the current level palette.")]
        private LevelColorPaletteAsset fallbackPalette;

        private readonly List<GameplayObjectiveView> _views = new();
        private IGameplaySessionHost _sessionHost;
        private IGameplayPresentationStatus _presentationStatus;
        private IGameplayState _state;
        private string _levelId;

        private void Start()
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

        private void OnDestroy()
        {
            UnbindHost();
        }

        /// <summary>
        /// Binds to an explicit session host. Tests and custom scene setup can call this directly.
        /// </summary>
        public void Initialize(IGameplaySessionHost sessionHost)
        {
            UnbindHost();

            _sessionHost = sessionHost;
            if (_sessionHost == null)
                return;

            _sessionHost.SessionStarted += BindSession;
            _presentationStatus = _sessionHost as IGameplayPresentationStatus;
            if (_presentationStatus != null)
                _presentationStatus.PresentationSettled += Render;

            if (_sessionHost.CurrentState != null)
                BindSession(_sessionHost.CurrentCommands, _sessionHost.CurrentState);
        }

        private void BindSession(IGameplayCommands commands, IGameplayState state)
        {
            UnbindState();

            _state = state;
            if (_state == null)
            {
                ClearViews();
                return;
            }

            if (_presentationStatus == null)
                _state.SnapshotChanged += Render;

            // Restart and mid-session board swaps (tutorial steps) change the objective items.
            _state.StateRestored += Rebuild;
            Rebuild(_state.CreateSnapshot());
        }

        private void Rebuild(GameSessionSnapshot snapshot)
        {
            ClearViews();
            _levelId = snapshot?.LevelId;

            ObjectiveProgress objectives = snapshot?.Objectives;
            if (objectives == null || viewCatalog == null)
                return;

            LevelColorPaletteAsset palette = ResolvePalette();
            Transform root = viewRoot != null ? viewRoot : transform;
            foreach (ObjectiveItemProgress item in objectives.Items)
            {
                GameplayObjectiveView view =
                    Instantiate(viewCatalog.GetRequiredObjectivePrefab(item.Target), root);
                view.Bind(item, palette);
                _views.Add(view);
            }
        }

        private void Render(GameSessionSnapshot snapshot)
        {
            ObjectiveProgress objectives = snapshot?.Objectives;
            if (objectives == null || snapshot.LevelId != _levelId)
                return;

            if (objectives.Items.Count != _views.Count)
            {
                Rebuild(snapshot);
                return;
            }

            for (int i = 0; i < _views.Count; i++)
                _views[i].Render(objectives.Items[i]);
        }

        private LevelColorPaletteAsset ResolvePalette()
        {
            LevelColorPaletteAsset palette = (_sessionHost as ILevelPaletteSource)?.CurrentPalette;
            return palette != null ? palette : fallbackPalette;
        }

        private void ClearViews()
        {
            foreach (GameplayObjectiveView view in _views)
            {
                if (view != null)
                    Destroy(view.gameObject);
            }

            _views.Clear();
        }

        private void UnbindHost()
        {
            UnbindState();

            if (_sessionHost != null)
                _sessionHost.SessionStarted -= BindSession;
            if (_presentationStatus != null)
                _presentationStatus.PresentationSettled -= Render;

            _sessionHost = null;
            _presentationStatus = null;
        }

        private void UnbindState()
        {
            if (_state != null)
            {
                _state.SnapshotChanged -= Render;
                _state.StateRestored -= Rebuild;
            }

            _state = null;
        }
    }
}
