using System;
using Blobs.Content;
using Blobs.Core;
using DG.Tweening;

namespace Blobs.Presentation
{
    /// <summary>
    /// Creates ordinary spawn, movement, and retirement transitions for blob views.
    /// Registry mutation remains owned by <see cref="BlobPresenter"/>.
    /// </summary>
    internal sealed class BlobTransitionPresenter
    {
        private readonly BlobPresenter _blobs;
        private readonly BlobMotionSettings _settings;

        public BlobTransitionPresenter(
            BlobPresenter blobs,
            BlobMotionSettings settings)
        {
            _blobs = blobs ?? throw new ArgumentNullException(nameof(blobs));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public bool TryCreate(
            BlobState blob,
            PresentationTimeline timeline,
            out Tween animation)
        {
            animation = null;
            if (!_blobs.TryCreateView(blob, out BlobView view))
                return false;

            if (timeline.IsAnimated)
                animation = view.PlaySpawn(_settings.SpawnDuration);
            return true;
        }

        public bool TryMove(
            string blobId,
            GridPosition to,
            Ease ease,
            PresentationTimeline timeline,
            Action onArrival,
            out Tween animation)
        {
            animation = null;
            if (!_blobs.TryGetView(blobId, out BlobView view))
                return false;

            if (!timeline.IsAnimated)
            {
                view.SetGridPosition(to);
                onArrival?.Invoke();
                return true;
            }

            Sequence movement = DOTween.Sequence();
            movement.AppendCallback(() => view.BlobMotionAnimator?.SetMoving());
            movement.Append(view.AnimateMoveTo(to, _settings.MoveDuration, ease));
            movement.OnComplete(() =>
            {
                view.BlobMotionAnimator?.SetIdle();
                onArrival?.Invoke();
            });
            animation = movement;
            return true;
        }

        public bool TryRemove(
            string blobId,
            PresentationTimeline timeline,
            out Tween animation)
        {
            animation = null;
            if (!_blobs.TryRetireView(blobId, out BlobView view))
                return false;

            if (!timeline.IsAnimated)
            {
                _blobs.DestroyRetiringView(view);
                return true;
            }

            Sequence retirement = DOTween.Sequence();
            retirement.Append(view.PlayDespawn(_settings.DespawnDuration));
            retirement.AppendCallback(() => _blobs.DestroyRetiringView(view));
            animation = retirement;
            return true;
        }
    }
}
