using System.Collections.Generic;
using Blobs.Application;
using Blobs.Content;
using Blobs.Core;
using DG.Tweening;
using UnityEngine;

namespace Blobs.Presentation
{
    /// <summary>
    /// Owns creation, tracking, animation, and removal of blob views.
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
        }

        /// <summary>
        /// Replaces all tracked views with views for the supplied authoritative blob states.
        /// </summary>
        public void Rebuild(IEnumerable<BlobState> blobs)
        {
            Clear();

            foreach (BlobState blob in blobs)
                Create(blob, null);
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

        public bool Create(BlobState blob, Sequence sequence)
        {
            if (blob == null || _views.ContainsKey(blob.Id) || _viewFactory == null)
                return false;

            Transform parent = blobRoot != null ? blobRoot : transform;
            BlobView view = _viewFactory.Create(
                blob,
                _state,
                parent,
                _cellSize,
                _origin);

            if (view == null)
                return false;

            _views.Add(blob.Id, view);
            sequence?.Append(view.PlaySpawn(spawnDuration));
            return true;
        }

        /// <summary>
        /// Creates a view immediately and joins its spawn animation to an existing move beat.
        /// </summary>
        public bool CreateJoined(BlobState blob, Sequence beat)
        {
            if (!Create(blob, null))
                return false;

            if (beat != null && TryGetView(blob.Id, out BlobView view))
                beat.Join(view.PlaySpawn(spawnDuration));

            return true;
        }

        public bool Move(string blobId, GridPosition to, Sequence sequence)
        {
            if (!TryGetView(blobId, out BlobView view))
                return false;

            if (sequence == null)
            {
                view.SetGridPosition(to);
                return true;
            }

            sequence.AppendCallback(() => view.BlobMotionAnimator?.SetMoving());
            Tween movement = view.AnimateMoveTo(to, moveDuration);
            movement.OnComplete(() => view.BlobMotionAnimator?.SetIdle());
            sequence.Append(movement);
            return true;
        }

        /// <summary>
        /// Moves a blob in parallel with the current beat and optionally invokes contact feedback on arrival.
        /// </summary>
        public bool MoveJoined(
            string blobId,
            GridPosition to,
            Sequence beat,
            Ease ease,
            System.Action onArrival)
        {
            if (!TryGetView(blobId, out BlobView view))
                return false;

            if (beat == null)
            {
                view.SetGridPosition(to);
                onArrival?.Invoke();
                return true;
            }

            beat.AppendCallback(() => view.BlobMotionAnimator?.SetMoving());
            Tween movement = view.AnimateMoveTo(to, moveDuration, ease);
            movement.OnComplete(() =>
            {
                view.BlobMotionAnimator?.SetIdle();
                onArrival?.Invoke();
            });
            beat.Join(movement);
            return true;
        }

        /// <summary>
        /// Immediately removes and destroys a tracked blob view.
        /// </summary>
        public bool Remove(string blobId)
        {
            return Remove(blobId, null);
        }

        public bool Remove(string blobId, Sequence sequence)
        {
            if (!TryGetView(blobId, out BlobView view))
                return false;

            _views.Remove(blobId);
            if (sequence == null)
            {
                DestroyView(view);
                return true;
            }

            _retiringViews.Add(view);
            sequence
                .Append(view.PlayDespawn(despawnDuration))
                .AppendCallback(() => DestroyRetiringView(view));
            return true;
        }

        /// <summary>
        /// Builds the combined source-move and target-retirement animation for a normal merge.
        /// </summary>
        public bool CreateNormalMergeBeat(
            MoveBlobEffect move,
            RemoveBlobEffect remove,
            Sequence sequence,
            bool appendToSequence,
            System.Action onContact = null)
        {
            if (!TryGetView(move.BlobId, out BlobView sourceView) ||
                !TryGetView(remove.BlobId, out BlobView targetView))
            {
                return false;
            }

            _views.Remove(remove.BlobId);
            if (sequence == null)
            {
                sourceView.SetGridPosition(move.To);
                sourceView.BlobMotionAnimator?.SetIdle();
                onContact?.Invoke();
                DestroyView(targetView);
                return true;
            }

            _retiringViews.Add(targetView);
            Vector2Int direction = new(
                move.To.X - move.From.X,
                move.To.Y - move.From.Y);
            Sequence mergeBeat = EnsureMergeAnimationOrchestrator().CreateMergeBeat(
                sourceView,
                targetView,
                direction,
                onContact,
                () => DestroyRetiringView(targetView));

            if (appendToSequence)
                sequence.Append(mergeBeat);
            else
                sequence.Join(mergeBeat);

            return true;
        }

        /// <summary>
        /// Retires the captured source immediately from tracking while its flag-capture animation completes.
        /// </summary>
        public bool MergeIntoFlag(
            MergeIntoFlagEffect effect,
            Sequence outerSequence,
            System.Action onContact = null)
        {
            if (!TryGetView(effect.SourceId, out BlobView sourceView) ||
                !TryGetView(effect.FlagId, out BlobView flagView))
            {
                return false;
            }

            _views.Remove(effect.SourceId);
            if (outerSequence == null)
            {
                sourceView.SetGridPosition(effect.To);
                onContact?.Invoke();
                DestroyView(sourceView);
                return true;
            }

            _retiringViews.Add(sourceView);
            Sequence captureBeat = DOTween.Sequence();
            captureBeat.AppendCallback(() =>
            {
                sourceView.BlobMotionAnimator?.SetMerging();
                flagView.BlobMotionAnimator?.SetMerging();
            });
            captureBeat.Append(sourceView.PlayConsumedInto(
                effect.To,
                moveDuration,
                despawnDuration));
            captureBeat.InsertCallback(moveDuration, () =>
            {
                EnsureMergeAnimationOrchestrator().PlayImpact(
                    flagView.transform.position,
                    sourceView.MergeEffectColor,
                    flagView);
                onContact?.Invoke();
            });

            Tween targetFeedback = flagView.PlaySourceAccepted(moveDuration + despawnDuration);
            if (targetFeedback != null)
                captureBeat.Join(targetFeedback);

            captureBeat.OnComplete(() =>
            {
                flagView.BlobMotionAnimator?.SetIdle();
                DestroyRetiringView(sourceView);
            });
            outerSequence.Append(captureBeat);
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

        private void DestroyRetiringView(BlobView view)
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
