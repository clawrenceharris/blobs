using System;
using System.Threading;
using Blobs.Content;
using Blobs.Core;
using Cysharp.Threading.Tasks;
using DG.Tweening;

namespace Blobs.Presentation
{
    /// <summary>One fade, one continuous return, and one materialization or Grave departure.</summary>
    internal sealed class GhostHauntPresenter
    {
        private readonly BlobPresenter _blobs;
        private readonly GhostReturnSettings _settings;
        public GhostHauntPresenter(BlobPresenter blobs, GhostReturnSettings settings)
        {
            _blobs = blobs;
            _settings = settings;

        }

        public bool Present(GhostHauntEffect effect, PresentationTimeline timeline, Action onContact)
        {
            if (!_blobs.TryGetView(effect.GhostId, out BlobView ghost)) return false;
            BlobView occupant = null;
            if (effect.LandingBlobId != null &&
                !_blobs.TryRetireView(effect.LandingBlobId, out occupant)) return false;

            if (!timeline.IsAnimated)
            {
                ghost.SetGridPosition(effect.HauntDestination);
                ghost.BlobRenderer?.FadeableVisual?.Restore();
                if (occupant != null) _blobs.DestroyRetiringView(occupant);
                onContact?.Invoke();
                return true;
            }

            timeline.AppendAsync(token => ReturnAsync(effect, ghost, occupant, onContact, token));
            return true;
        }

        public bool PresentReverse(GhostHauntEffect effect, BoardEffectPresentationContext context)
        {
            if (!_blobs.TryGetView(effect.GhostId, out BlobView ghost))
                return false;

            BlobState landing = context.ResolveRestoredBlob(effect.LandingBlobId, effect.LandingBlob);
            GridPosition origin = ResolveHauntOrigin(effect);

            if (!context.Timeline.IsAnimated)
            {
                ghost.SetGridPosition(origin);
                ghost.BlobRenderer?.FadeableVisual?.Restore();
                if (landing != null && !_blobs.TryCreateView(landing, out _))
                    return false;
                return true;
            }

            context.Timeline.AppendAsync(token => UndoReturnAsync(effect, ghost, landing, origin, token));
            return true;
        }

        private async UniTask UndoReturnAsync(
            GhostHauntEffect effect,
            BlobView ghost,
            BlobState landing,
            GridPosition origin,
            CancellationToken token)
        {
            FadeableVisual fade = ghost.BlobRenderer?.FadeableVisual;
            try
            {
                if (fade != null)
                    await fade.FadeTo(0f, _settings.FadeDuration, token);
                await PresentationTimeline.AwaitTweenAsync(
                    ghost.AnimateMoveTo(origin, _settings.MoveDuration * effect.Path.Count, Ease.Linear),
                    token);
                if (landing != null)
                {
                    if (!_blobs.TryCreateView(landing, out BlobView occupant))
                        return;
                    await PresentationTimeline.AwaitTweenAsync(
                        occupant.PlaySpawn(_settings.DespawnDuration), token);
                }
                if (fade != null)
                    await fade.FadeTo(1f, _settings.FadeDuration, token);
                ghost.BlobMotionAnimator?.SetIdle();
            }
            finally
            {
                if (fade != null && token.IsCancellationRequested)
                    fade.Restore();
            }
        }

        private async UniTask ReturnAsync(GhostHauntEffect effect, BlobView ghost,
            BlobView occupant, Action onContact, CancellationToken token)
        {
            FadeableVisual fade = ghost.BlobRenderer?.FadeableVisual;
            try
            {
                if (fade != null) await fade.FadeTo(0f, _settings.FadeDuration, token);
                // A straight linear tween preserves constant speed across logical path cells.
                await PresentationTimeline.AwaitTweenAsync(ghost.AnimateMoveTo(
                    effect.HauntDestination, _settings.MoveDuration * effect.Path.Count, Ease.Linear), token);
                if (occupant != null)
                {
                    await PresentationTimeline.AwaitTweenAsync(
                        occupant.PlayDespawn(_settings.DespawnDuration), token);
                    _blobs.DestroyRetiringView(occupant);
                }
                if (fade != null)
                    await fade.FadeTo(1f, _settings.FadeDuration, token);
                ghost.BlobMotionAnimator?.SetIdle();
                onContact?.Invoke();
            }
            finally
            {
                // A successful Grave return stays ethereal until the clear beat.
                if (fade != null && token.IsCancellationRequested)
                    fade.Restore();
            }
        }

        private static GridPosition ResolveHauntOrigin(GhostHauntEffect effect)
        {
            if (effect.HasOrigin)
                return effect.From;
            if (effect.Path.Count >= 2)
                return effect.Path[0] - (effect.Path[1] - effect.Path[0]);
            return effect.Path[0];
        }
    }
}