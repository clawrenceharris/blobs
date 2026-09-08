using Blobs.Core;
using DG.Tweening;

namespace Blobs.Presentation
{
    internal sealed class RemoveBlobPresentationHandler :
        BoardEffectPresentationHandler<RemoveBlobEffect>
    {
        public override BoardEffectPresentationPhase Phase =>
            BoardEffectPresentationPhase.Arrival;

        protected override bool PresentOrdered(
            RemoveBlobEffect effect,
            BoardEffectPresentationContext context)
        {
            return PresentRemoval(effect, context);
        }

        protected override bool PresentInBeat(
            RemoveBlobEffect effect,
            BoardEffectPresentationContext context)
        {
            return PresentRemoval(effect, context);
        }

        private static bool PresentRemoval(
            RemoveBlobEffect effect,
            BoardEffectPresentationContext context)
        {
            if (!context.Blobs.Transitions.TryRemove(
                    effect.BlobId,
                    context.Timeline,
                    out Tween animation))
            {
                return false;
            }

            context.Timeline.Append(animation);
            return true;
        }
    }
}
