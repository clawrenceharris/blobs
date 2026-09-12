using System;
using System.Threading;
using Blobs.Content;
using Blobs.Core;
using Cysharp.Threading.Tasks;
using DG.Tweening;

namespace Blobs.Presentation
{
    /// <summary>One fade, one continuous return, and one materialization or Grave departure.</summary>
    internal sealed class GhostRestPresenter
    {
        private readonly BlobPresenter _blobs;
        private readonly GhostReturnSettings _settings;
        public GhostRestPresenter(BlobPresenter blobs, GhostReturnSettings settings)
        {
            _blobs = blobs;
            _settings = settings;

        }

        public bool Present(GhostRestEffect effect, PresentationTimeline timeline, Action onContact)
        {
            if (!_blobs.TryRetireView(effect.GhostId, out BlobView ghost)) return false;
            BlobView occupant = null;
            if (effect.LandingBlobId != null &&
                !_blobs.TryRetireView(effect.LandingBlobId, out occupant)) return false;


            if (!timeline.IsAnimated)
            {
                ghost.SetGridPosition(effect.RestDestination);
                ghost.BlobRenderer?.FadeableVisual?.Restore();
                if (occupant != null) _blobs.DestroyRetiringView(occupant);
                _blobs.DestroyRetiringView(ghost);
                onContact?.Invoke();
                return true;
            }

            timeline.AppendAsync(token => ReturnAsync(effect, ghost, occupant, onContact, token));
            return true;
        }

        public bool PresentReverse(GhostRestEffect effect, BoardEffectPresentationContext context)
        {
            BlobState ghostState = context.ResolveRestoredBlob(effect.GhostId, effect.GhostBlob);
            if (ghostState == null)
                return false;

            BlobState landing = context.ResolveRestoredBlob(effect.LandingBlobId, effect.LandingBlob);
            BlobState ghostAtRest = ghostState.WithPosition(effect.RestDestination);
            if (!_blobs.TryCreateView(ghostAtRest, out BlobView ghost))
                return false;

            if (!context.Timeline.IsAnimated)
            {
                ghost.SetGridPosition(ghostState.Position);
                ghost.BlobRenderer?.FadeableVisual?.Restore();
                if (landing != null && !_blobs.TryCreateView(landing, out _))
                    return false;
                return true;
            }

            context.Timeline.AppendAsync(token => UndoReturnAsync(effect, ghost, ghostState, landing, token));
            return true;
        }

        private async UniTask UndoReturnAsync(
            GhostRestEffect effect,
            BlobView ghost,
            BlobState ghostState,
            BlobState landing,
            CancellationToken token)
        {
            FadeableVisual fade = ghost.BlobRenderer?.FadeableVisual;
            try
            {
                await PresentationTimeline.AwaitTweenAsync(
                    ghost.PlaySpawn(_settings.DespawnDuration), token);
                if (landing != null)
                {
                    if (!_blobs.TryCreateView(landing, out BlobView occupant))
                        return;
                    await PresentationTimeline.AwaitTweenAsync(
                        occupant.PlaySpawn(_settings.DespawnDuration), token);
                }
                if (fade != null)
                    await fade.FadeTo(0f, _settings.FadeDuration, token);
                await PresentationTimeline.AwaitTweenAsync(
                    ghost.AnimateMoveTo(
                        ghostState.Position,
                        _settings.MoveDuration * Math.Max(1, effect.Path.Count),
                        Ease.Linear),
                    token);
                if (fade != null)
                    await fade.FadeTo(1f, _settings.FadeDuration, token);
                ghost.BlobMotionAnimator?.SetIdle();
            }
            finally
            {
                if (ghost != null && fade != null && token.IsCancellationRequested)
                    fade.Restore();
            }
        }

        private async UniTask ReturnAsync(GhostRestEffect effect, BlobView ghost, BlobView occupant, Action onContact, CancellationToken token)
        {
            FadeableVisual fade = ghost.BlobRenderer?.FadeableVisual;
            try
            {
                if (fade != null) await fade.FadeTo(0f, _settings.FadeDuration, token);
                // A straight linear tween preserves constant speed across logical path cells.
                await PresentationTimeline.AwaitTweenAsync(ghost.AnimateMoveTo(
                    effect.RestDestination, _settings.MoveDuration * effect.Path.Count, Ease.Linear), token);

                // Reveal the Ghost at its resting place before despawning it.
                // Destruction must be the final operation on this view.
                if (fade != null)
                    await fade.FadeTo(1f, _settings.FadeDuration, token);
                if (occupant != null)
                {
                    await PresentationTimeline.AwaitTweenAsync(
                        occupant.PlayDespawn(_settings.DespawnDuration), token);
                    _blobs.DestroyRetiringView(occupant);
                }
                onContact?.Invoke();
                token.ThrowIfCancellationRequested();
                await PresentationTimeline.AwaitTweenAsync(
                    ghost.PlayDespawn(_settings.DespawnDuration), token);
                _blobs.DestroyRetiringView(ghost);
            }
            finally
            {
                // Restart owns cleanup of retired views; do not touch a destroyed view.
                if (ghost != null && fade != null && token.IsCancellationRequested)
                    fade.Restore();
            }
        }
    }
}