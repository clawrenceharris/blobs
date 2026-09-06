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
    public sealed class BlobPresenter : MonoBehaviour
    {
        private readonly Dictionary<string, BlobView> _views = new();
        private readonly List<BlobView> _retiringViews = new();

        [SerializeField] private BlobViewCatalogAsset _blobViewCatalog;
        [SerializeField] private Transform blobRoot;
        [SerializeField, Min(0f)] private float moveDuration = 0.16f;
        [SerializeField, Min(0f)] private float spawnDuration = 0.14f;
        [SerializeField, Min(0f)] private float despawnDuration = 0.12f;
        [SerializeField] private MergeAnimationOrchestrator _mergeAnimationOrchestrator;

        private IGameplayState _state;
        private IBlobViewFactory _viewFactory;
        private float _cellSize;
        private Vector2 _origin;

        public int VisibleCount => _views.Count;
        internal BlobTransitionPresenter Transitions { get; private set; }
        internal NormalMergePresenter NormalMerges { get; private set; }
        internal FlagCapturePresenter FlagCaptures { get; private set; }

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
            Clear();
            _state = state;
            _cellSize = cellSize;
            _origin = origin;
            _viewFactory = viewFactory ?? new BlobViewFactory(_blobViewCatalog, palette);

            MergeAnimationOrchestrator orchestrator = EnsureMergeAnimationOrchestrator();
            Transitions = new BlobTransitionPresenter(
                this,
                moveDuration,
                spawnDuration,
                despawnDuration);
            NormalMerges = new NormalMergePresenter(this, orchestrator);
            FlagCaptures = new FlagCapturePresenter(
                this,
                orchestrator,
                moveDuration,
                despawnDuration);
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
            return _views.TryGetValue(blobId, out view) && view != null;
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
        /// Checks tracked blob identities and logical positions without relying on tween completion.
        /// </summary>
        public bool IsSynchronizedWith(IEnumerable<BlobState> blobs, int expectedCount)
        {
            if (_views.Count != expectedCount)
                return false;

            foreach (BlobState blob in blobs)
            {
                if (!TryGetView(blob.Id, out BlobView view) ||
                    view.BlobId != blob.Id ||
                    view.GridPosition != blob.Position)
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
                _state,
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

                view.SetGridPosition(view.GridPosition);
                view.BlobMotionAnimator?.SetIdle();
            }
        }

        public void Clear()
        {
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

        private MergeAnimationOrchestrator EnsureMergeAnimationOrchestrator()
        {
            if (_mergeAnimationOrchestrator != null)
                return _mergeAnimationOrchestrator;

            _mergeAnimationOrchestrator = GetComponent<MergeAnimationOrchestrator>();
            if (_mergeAnimationOrchestrator == null)
                _mergeAnimationOrchestrator = gameObject.AddComponent<MergeAnimationOrchestrator>();
            return _mergeAnimationOrchestrator;
        }
    }
}
