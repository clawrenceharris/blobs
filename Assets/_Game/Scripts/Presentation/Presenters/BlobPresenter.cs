using System.Collections.Generic;
using Blobs.Application;
using Blobs.Content;
using Blobs.Core;
using DG.Tweening;
using UnityEngine;

namespace Blobs.Presentation
{
    /// <summary>
    /// Owns creation, identity tracking, retirement, and cleanup of blob views.
    /// Focused collaborators own transition and interaction choreography.
    /// </summary>
    [RequireComponent(typeof(MergeAnimationOrchestrator))]
    public sealed class BlobPresenter : MonoBehaviour
    {
        private readonly Dictionary<string, BlobView> _views = new();
        private readonly List<BlobView> _retiringViews = new();
        [SerializeField] private BlobAnimationSettingsAsset blobAnimationSettings;

        [SerializeField] private ViewCatalogAsset _viewCatalog;
        [SerializeField] private Transform blobRoot;
        [SerializeField] private MergeAnimationOrchestrator _mergeAnimationOrchestrator;

        private IBlobViewFactory _viewFactory;
        private BlobSelectionPresenter _selectionPresenter;
        private float _cellSize;
        private Vector2 _origin;

        public int VisibleCount => _views.Count;
        internal BlobTransitionPresenter Transitions { get; private set; }
        internal MergePresenter Merges { get; private set; }

        internal GhostReturnPresenter GhostReturns { get; private set; }

        /// <summary>
        /// Configures view creation for the active gameplay session and clears views from any prior session.
        /// </summary>
        public void Initialize(
            IGameplayState state,
            LevelColorPaletteAsset palette,
            float cellSize,
            Vector2 origin,
            IBlobViewFactory viewFactory = null)
        {
            if (state == null)
                throw new System.ArgumentNullException(nameof(state));

            DisconnectFromState();
            Clear();
            _cellSize = cellSize;
            _origin = origin;
            _viewFactory = viewFactory ?? new BlobViewFactory(_viewCatalog, palette);

            MergeAnimationOrchestrator orchestrator = ResolveMergeAnimationOrchestrator();
            Transitions = new BlobTransitionPresenter(
                this,
                blobAnimationSettings.BlobMotionSettings);
            GhostReturns = new GhostReturnPresenter(this, blobAnimationSettings.GhostReturnSettings);
            Merges = new MergePresenter(this, orchestrator,
                blobAnimationSettings.MergeImpactSettings, blobAnimationSettings.BlobMotionSettings);
            _selectionPresenter = new BlobSelectionPresenter(this, state);
        }

        /// <summary>
        /// Replaces all tracked views with views for the supplied authoritative blob states.
        /// </summary>
        public void Rebuild(IEnumerable<BlobState> blobs)
        {
            Clear();

            foreach (BlobState blob in blobs)
                TryCreateView(blob, out _);
        }

        public bool TryGetView(string blobId, out BlobView view)
        {
            view = null;
            return !string.IsNullOrEmpty(blobId) &&
                _views.TryGetValue(blobId, out view) &&
                view != null;
        }

        /// <summary>
        /// Captures a target's prefab-composed contact feedback before effects can retire its view.
        /// </summary>
        public System.Action CreateContactFeedback(string sourceBlobId, string targetBlobId)
        {
            if (!TryGetView(targetBlobId, out BlobView targetView))
                return null;

            TryGetView(sourceBlobId, out BlobView sourceView);
            return () =>
            {
                if (targetView != null)
                    targetView.PlayContactFeedback(sourceView);
            };
        }

        /// <summary>
        /// Checks all render-relevant blob state without relying on tween completion.
        /// </summary>
        public bool IsSynchronizedWith(IEnumerable<BlobState> blobs, int expectedCount)
        {
            if (_views.Count != expectedCount)
                return false;

            foreach (BlobState blob in blobs)
            {
                if (!TryGetView(blob.Id, out BlobView view) ||
                    !view.IsPresenting(blob))
                {
                    return false;
                }
            }

            return true;
        }

        internal bool TryCreateView(BlobState blob, out BlobView view)
        {
            view = null;
            if (blob == null || _views.ContainsKey(blob.Id) || _viewFactory == null)
                return false;

            Transform parent = blobRoot != null ? blobRoot : transform;
            view = _viewFactory.Create(
                blob,
                parent,
                _cellSize,
                _origin);

            if (view == null)
                return false;

            _views.Add(blob.Id, view);
            return true;
        }

        /// <summary>
        /// Immediately removes and destroys a tracked blob view.
        /// </summary>
        public bool Remove(string blobId)
        {
            if (!TryRetireView(blobId, out BlobView view))
                return false;

            DestroyRetiringView(view);
            return true;
        }

        internal bool TryRetireView(string blobId, out BlobView view)
        {
            if (!TryGetView(blobId, out view))
                return false;

            _views.Remove(blobId);
            _retiringViews.Add(view);
            return true;
        }

        /// <summary>
        /// Resolves views left between states when their owning DOTween sequence is interrupted.
        /// </summary>
        public void CompleteInterruptedAnimations()
        {
            DestroyRetiringViews();

            foreach (BlobView view in _views.Values)
            {
                if (view == null)
                    continue;

                view.BlobRenderer?.FadeableVisual?.Restore();
                view.SetGridPosition(view.GridPosition);
                view.BlobMotionAnimator?.SetIdle();
            }
        }

        public void Clear()
        {
            _selectionPresenter?.ClearSelection();
            DestroyRetiringViews();

            foreach (BlobView view in _views.Values)
            {
                if (view == null)
                    continue;

                view.transform.DOKill();
                DestroyView(view);
            }

            _views.Clear();
        }

        /// <summary>Releases the gameplay-state subscription owned for the active session.</summary>
        internal void DisconnectFromState()
        {
            _selectionPresenter?.Dispose();
            _selectionPresenter = null;
        }

        private void DestroyRetiringViews()
        {
            for (int i = _retiringViews.Count - 1; i >= 0; i--)
                DestroyRetiringView(_retiringViews[i]);

            _retiringViews.Clear();
        }

        internal void DestroyRetiringView(BlobView view)
        {
            _retiringViews.Remove(view);
            DestroyView(view);
        }

        private static void DestroyView(BlobView view)
        {
            if (view == null)
                return;

            if (UnityEngine.Application.isPlaying)
                Destroy(view.gameObject);
            else
                DestroyImmediate(view.gameObject);
        }

        private MergeAnimationOrchestrator ResolveMergeAnimationOrchestrator()
        {
            if (_mergeAnimationOrchestrator != null)
                return _mergeAnimationOrchestrator;

            _mergeAnimationOrchestrator = GetComponent<MergeAnimationOrchestrator>();
            if (_mergeAnimationOrchestrator == null)
            {
                throw new System.InvalidOperationException(
                    "BlobPresenter requires an authored MergeAnimationOrchestrator " +
                    "on the same GameObject.");
            }

            return _mergeAnimationOrchestrator;
        }

        private void OnDestroy()
        {
            DisconnectFromState();
        }
    }
}
