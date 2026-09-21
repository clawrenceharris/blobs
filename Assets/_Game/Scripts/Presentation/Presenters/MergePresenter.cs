using System;
using Blobs.Content;
using Blobs.Core;
using DG.Tweening;
using UnityEngine;

namespace Blobs.Presentation
{
    /// <summary>
    /// Presents an explicit two-blob merge. Participant roles come from Core; this class
    /// never infers an interaction from neighboring effects or from a blob's type.
    /// BlobPresenter owns view identity/retirement and supplies authored animation settings.
    /// </summary>
    internal sealed class MergePresenter
    {
        private readonly BlobPresenter _blobs;
        private readonly MergeAnimationOrchestrator _orchestrator;
        private readonly BlobMergeImpactSettings _mergeSettings;
        private readonly BlobMotionSettings _motionSettings;

        public MergePresenter(BlobPresenter blobs, MergeAnimationOrchestrator orchestrator,
            BlobMergeImpactSettings impactSettings, BlobMotionSettings motionSettings)
        {
            _blobs = blobs ?? throw new ArgumentNullException(nameof(blobs));
            _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
            _mergeSettings = impactSettings ?? throw new ArgumentNullException(nameof(impactSettings));
            _motionSettings = motionSettings ?? throw new ArgumentNullException(nameof(motionSettings));
        }

        public bool Present(MergeEffect effect, PresentationTimeline timeline, Action onContact = null)
        {
            // Validate all participants before retiring a view. An unsupported effect lets
            // BoardPresenter recover from its authoritative snapshot without partial choreography.
            bool moverSurvives = effect.SurvivingBlobId == effect.MovingBlobId &&
                effect.ConsumedBlobId == effect.TargetBlobId;
            bool targetSurvives = effect.SurvivingBlobId == effect.TargetBlobId &&
                effect.ConsumedBlobId == effect.MovingBlobId;
            if (effect.MovingBlobId == effect.TargetBlobId || (!moverSurvives && !targetSurvives) ||
                !_blobs.TryGetView(effect.MovingBlobId, out BlobView moving) ||
                !_blobs.TryGetView(effect.TargetBlobId, out BlobView target) ||
                !_blobs.TryRetireView(effect.ConsumedBlobId, out BlobView consumed))
            {
                return false;
            }

            BlobView survivor = moverSurvives ? moving : target;
            if (!timeline.IsAnimated)
            {
                moving.SetGridPosition(effect.To);
                survivor.BlobMotionAnimator?.SetIdle();
                onContact?.Invoke();
                _blobs.DestroyRetiringView(consumed);
                return true;
            }

            // Both survivor outcomes share the same approach/contact beat. Ghost return and
            // other post-merge behavior remain separate Core effects and presentation handlers.
            timeline.Append(_orchestrator.CreateMergeBeat(
                moving,
                target,
                moverSurvives,
                new Vector2Int(effect.To.X - effect.From.X, effect.To.Y - effect.From.Y),
                onContact,
                () => _blobs.DestroyRetiringView(consumed),
                _mergeSettings,
                effect.To));
            return true;
        }

        public bool PresentReverse(MergeEffect effect, BoardEffectPresentationContext context)
        {
            bool moverSurvives = effect.SurvivingBlobId == effect.MovingBlobId &&
                effect.ConsumedBlobId == effect.TargetBlobId;
            bool targetSurvives = effect.SurvivingBlobId == effect.TargetBlobId &&
                effect.ConsumedBlobId == effect.MovingBlobId;
            BlobState consumed = context.ResolveRestoredBlob(effect.ConsumedBlobId, effect.ConsumedBlob);
            if ((!moverSurvives && !targetSurvives) || consumed == null)
                return false;

            GridPosition restoreAt = moverSurvives ? effect.At : effect.From;
            BlobState restoredState = consumed.WithPosition(restoreAt);
            if (!_blobs.TryCreateView(restoredState, out BlobView restored))
                return false;

            PresentationTimeline timeline = context.Timeline;
            if (!timeline.IsAnimated)
            {
                if (moverSurvives && _blobs.TryGetView(effect.MovingBlobId, out BlobView mover))
                    mover.SetGridPosition(effect.From);
                restored.SetGridPosition(restoreAt);
                return true;
            }

            Sequence undo = DOTween.Sequence();
            Tween spawn = restored.PlaySpawn(_motionSettings.SpawnDuration);
            if (spawn != null)
                undo.Join(spawn);

            if (moverSurvives)
            {
                if (!_blobs.TryGetView(effect.MovingBlobId, out BlobView mover))
                    return false;
                undo.Join(mover.AnimateMoveTo(effect.From, _motionSettings.MoveDuration, Ease.OutQuad));
                undo.OnComplete(() => mover.BlobMotionAnimator?.SetIdle());
            }

            timeline.Append(undo);
            return true;
        }
    }
}
