using System;
using System.Threading;
using Blobs.Content;
using Blobs.Core;
using Cysharp.Threading.Tasks;
using DG.Tweening;

namespace Blobs.Presentation
{
    /// <summary>One fade, one continuous return, and one materialization or Sigil departure.</summary>
    internal sealed class GhostReturnPresenter
    {
        private readonly BlobPresenter _blobs;
        private readonly GhostReturnSettings _settings;
        public GhostReturnPresenter(BlobPresenter blobs, GhostReturnSettings settings)
        {
            _blobs = blobs;
            _settings = settings;

        }

        public bool Present(GhostReturnEffect effect, PresentationTimeline timeline, Action onContact)
        {
            if (!_blobs.TryGetView(effect.GhostId, out BlobView ghost)) return false;
            BlobView occupant = null;
            if (effect.LandingBlobId != null &&
                !_blobs.TryRetireView(effect.LandingBlobId, out occupant)) return false;

            if (!timeline.IsAnimated)
            {
                ghost.SetGridPosition(effect.LastStep.Position);
                ghost.BlobRenderer?.FadeableVisual?.Restore();
                if (occupant != null) _blobs.DestroyRetiringView(occupant);
                onContact?.Invoke();
                return true;
            }

            timeline.AppendAsync(token => ReturnAsync(effect, ghost, occupant, onContact, token));
            return true;
        }

        private async UniTask ReturnAsync(GhostReturnEffect effect, BlobView ghost,
            BlobView occupant, Action onContact, CancellationToken token)
        {
            FadeableVisual fade = ghost.BlobRenderer?.FadeableVisual;
            try
            {
                if (fade != null) await fade.FadeTo(0f, _settings.FadeDuration, token);
                // A straight linear tween preserves constant speed across logical path cells.
                await PresentationTimeline.AwaitTweenAsync(ghost.AnimateMoveTo(
                    effect.LastStep.Position, _settings.MoveDuration * effect.Steps.Count, Ease.Linear), token);
                if (occupant != null)
                {
                    await PresentationTimeline.AwaitTweenAsync(
                        occupant.PlayDespawn(_settings.DespawnDuration), token);
                    _blobs.DestroyRetiringView(occupant);
                }
                if (!effect.IsClearing && fade != null)
                    await fade.FadeTo(1f, _settings.FadeDuration, token);
                ghost.BlobMotionAnimator?.SetIdle();
                onContact?.Invoke();
            }
            finally
            {
                // A successful Sigil return stays ethereal until the clear beat.
                if (fade != null && (!effect.IsClearing || token.IsCancellationRequested))
                    fade.Restore();
            }
        }
    }
}
