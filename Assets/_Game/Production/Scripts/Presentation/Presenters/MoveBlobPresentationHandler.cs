using System;
using Blobs.Core;
using DG.Tweening;

namespace Blobs.Presentation
{
    internal sealed class MoveBlobPresentationHandler :
        BoardEffectPresentationHandler<MoveBlobEffect>
    {
        public override BoardEffectPresentationPhase Phase =>
            BoardEffectPresentationPhase.Travel;
        public override bool RepresentsMovement => true;

        protected override bool PresentOrdered(
            MoveBlobEffect effect,
            BoardEffectPresentationContext context)
        {
            return PresentMove(effect.BlobId, effect.To, Ease.Linear, context, onArrival: null);
        }

        protected override bool PresentInBeat(
            MoveBlobEffect effect,
            BoardEffectPresentationContext context)
        {
            return PresentMove(
                effect.BlobId,
                effect.To,
                context.MovementEase,
                context,
                context.ContactFeedback);
        }

        protected override bool PresentReverse(
            MoveBlobEffect effect,
            BoardEffectPresentationContext context)
        {
            return PresentMove(effect.BlobId, effect.From, Ease.Linear, context, onArrival: null);
        }

        protected override bool PresentReverseInBeat(
            MoveBlobEffect effect,
            BoardEffectPresentationContext context)
        {
            return PresentMove(effect.BlobId, effect.From, context.MovementEase, context, onArrival: null);
        }

        private static bool PresentMove(
            string blobId,
            GridPosition to,
            Ease ease,
            BoardEffectPresentationContext context,
            Action onArrival)
        {
            if (!context.Blobs.Transitions.TryMove(
                    blobId,
                    to,
                    ease,
                    context.Timeline,
                    onArrival,
                    out Tween animation))
            {
                return false;
            }

            context.Timeline.Append(animation);
            return true;
        }
    }
}
