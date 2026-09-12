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

        protected override bool PresentReverse(
            RemoveBlobEffect effect,
            BoardEffectPresentationContext context)
        {
            return PresentRestore(effect, context, join: false);
        }

        protected override bool PresentReverseInBeat(
            RemoveBlobEffect effect,
            BoardEffectPresentationContext context)
        {
            return PresentRestore(effect, context, join: false);
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

        private static bool PresentRestore(
            RemoveBlobEffect effect,
            BoardEffectPresentationContext context,
            bool join)
        {
            BlobState restored = context.ResolveRestoredBlob(effect.BlobId, effect.Blob);
            if (restored == null)
                return false;

            if (!context.Blobs.Transitions.TryCreate(
                    restored.WithPosition(effect.At),
                    context.Timeline,
                    out Tween animation))
            {
                return false;
            }

            if (join)
                context.Timeline.Join(animation);
            else
                context.Timeline.Append(animation);
            return true;
        }
    }
}
