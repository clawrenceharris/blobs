using System;
using Blobs.Content;
using Blobs.Core;
using DG.Tweening;
using UnityEngine;

namespace Blobs.Presentation
{
    /// <summary>
    /// Nest-only choreography: approach the nest, then seat the retired monster.
    /// Normal merges stay in <see cref="MergePresenter"/>; this type does not share
    /// survivor branching because nest captures always consume the mover.
    /// </summary>
    internal sealed class MergeIntoNestPresenter
    {
        private readonly BlobPresenter _blobs;
        private readonly MergeAnimationOrchestratorBase _orchestrator;
        private readonly BlobMergeImpactSettings _mergeSettings;
        private readonly BlobMotionSettings _motionSettings;

        public MergeIntoNestPresenter(
            BlobPresenter blobs,
            MergeAnimationOrchestratorBase orchestrator,
            BlobMergeImpactSettings impactSettings,
            BlobMotionSettings motionSettings)
        {
            _blobs = blobs ?? throw new ArgumentNullException(nameof(blobs));
            _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
            _mergeSettings = impactSettings ?? throw new ArgumentNullException(nameof(impactSettings));
            _motionSettings = motionSettings ?? throw new ArgumentNullException(nameof(motionSettings));
        }

        public bool Present(
            MergeIntoNestEffect effect,
            PresentationTimeline timeline,
            Action onContact = null)
        {
            if (effect.SourceId == effect.NestId ||
                !_blobs.TryGetView(effect.SourceId, out BlobView source) ||
                !_blobs.TryGetView(effect.NestId, out BlobView nest) ||
                !NestBlobView.TryResolve(nest, out NestBlobView nestView) ||
                !_blobs.TryRetireView(effect.SourceId, out BlobView consumed))
            {
                return false;
            }

            if (!timeline.IsAnimated)
            {
                source.SetGridPosition(effect.To);
                nest.BlobMotionAnimator?.SetIdle();
                onContact?.Invoke();
                NestVictorySequence.ApplyInstant(nestView, consumed);
                _blobs.AbandonRetiringView(consumed);
                return true;
            }

            timeline.Append(_orchestrator.CreateMergeBeat(
                source,
                nest,
                false,
                new Vector2Int(effect.To.X - effect.From.X, effect.To.Y - effect.From.Y),
                onContact,
                onTargetConsumed: null,
                _mergeSettings,
                effect.To));

            timeline.Append(NestVictorySequence.Create(
                nestView,
                consumed,
                () => _blobs.AbandonRetiringView(consumed)));

            return true;
        }

        public bool PresentReverse(
            MergeIntoNestEffect effect,
            BoardEffectPresentationContext context)
        {
            BlobState consumed = context.ResolveRestoredBlob(effect.SourceId, effect.SourceBlob);
            if (consumed == null)
                return false;

            if (_blobs.TryGetView(effect.NestId, out BlobView nestTarget) &&
                NestBlobView.TryResolve(nestTarget, out NestBlobView nest))
            {
                nest.ClearOccupation(destroyOccupant: true);
            }

            BlobState restoredState = consumed.WithPosition(effect.From);
            if (!_blobs.TryCreateView(restoredState, out BlobView restored))
                return false;

            PresentationTimeline timeline = context.Timeline;
            if (!timeline.IsAnimated)
            {
                restored.SetGridPosition(effect.From);
                return true;
            }

            Sequence undo = DOTween.Sequence();
            Tween spawn = restored.PlaySpawn(_motionSettings.SpawnDuration);
            if (spawn != null)
                undo.Join(spawn);

            timeline.Append(undo);
            return true;
        }
    }
}
