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
    }
}