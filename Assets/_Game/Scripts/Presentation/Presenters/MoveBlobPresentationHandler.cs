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
            if (!context.Blobs.Transitions.TryMove(
                    effect.BlobId,
                    effect.To,
                    Ease.Linear,
                    context.Timeline,
                    onArrival: null,
                    out Tween animation))
            {
                return false;
            }

            context.Timeline.Append(animation);
            return true;
        }

        protected override bool PresentInBeat(
            MoveBlobEffect effect,
            BoardEffectPresentationContext context)
        {
            if (!context.Blobs.Transitions.TryMove(
                    effect.BlobId,
                    effect.To,
                    context.MovementEase,
                    context.Timeline,
                    context.ContactFeedback,
                    out Tween animation))
            {
                return false;
            }

            context.Timeline.Append(animation);
            return true;
        }
    }
}
