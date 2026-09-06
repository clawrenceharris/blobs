using System;
using Blobs.Core;
using DG.Tweening;

namespace Blobs.Presentation
{
    /// <summary>
    /// Owns flag-capture choreography while the blob presenter owns view tracking and retirement.
    /// </summary>
    internal sealed class FlagCapturePresenter
    {
        private readonly BlobPresenter _blobs;
        private readonly MergeAnimationOrchestrator _orchestrator;
        private readonly float _moveDuration;
        private readonly float _despawnDuration;

        public FlagCapturePresenter(
            BlobPresenter blobs,
            MergeAnimationOrchestrator orchestrator,
            float moveDuration,
            float despawnDuration)
        {
            _blobs = blobs ?? throw new ArgumentNullException(nameof(blobs));
            _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
            _moveDuration = moveDuration;
            _despawnDuration = despawnDuration;
        }

        public bool Present(
            MergeIntoFlagEffect effect,
            PresentationTimeline timeline,
            Action onContact = null)
        {
            if (!_blobs.TryGetView(effect.FlagId, out BlobView flagView) ||
                !_blobs.TryRetireView(effect.SourceId, out BlobView sourceView))
            {
                return false;
            }

            if (!timeline.IsAnimated)
            {
                sourceView.SetGridPosition(effect.To);
                onContact?.Invoke();
                _blobs.DestroyRetiringView(sourceView);
                return true;
            }

            Sequence capture = DOTween.Sequence();
            capture.AppendCallback(() =>
            {
                sourceView.BlobMotionAnimator?.SetMerging();
                flagView.BlobMotionAnimator?.SetMerging();
            });
            capture.Append(sourceView.PlayConsumedInto(
                effect.To,
                _moveDuration,
                _despawnDuration));
            capture.InsertCallback(_moveDuration, () =>
            {
                _orchestrator.PlayImpact(
                    flagView.transform.position,
                    sourceView.MergeEffectColor,
                    flagView);
                onContact?.Invoke();
            });

            Tween targetFeedback = flagView.PlaySourceAccepted(
                _moveDuration + _despawnDuration);
            if (targetFeedback != null)
                capture.Join(targetFeedback);

            capture.OnComplete(() =>
            {
                flagView.BlobMotionAnimator?.SetIdle();
                _blobs.DestroyRetiringView(sourceView);
            });
            timeline.Append(capture);
            return true;
        }
    }
}
