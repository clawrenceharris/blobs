using Blobs.Core;
using DG.Tweening;

namespace Blobs.Presentation
{
    internal sealed class SpawnBlobPresentationHandler :
        BoardEffectPresentationHandler<SpawnBlobEffect>
    {
        public override BoardEffectPresentationPhase Phase =>
            BoardEffectPresentationPhase.Departure;

        protected override bool PresentOrdered(
            SpawnBlobEffect effect,
            BoardEffectPresentationContext context)
        {
            if (!context.Blobs.Transitions.TryCreate(
                    effect.Blob,
                    context.Timeline,
                    out Tween animation))
            {
                return false;
            }

            context.Timeline.Append(animation);
            return true;
        }

        protected override bool PresentInBeat(
            SpawnBlobEffect effect,
            BoardEffectPresentationContext context)
        {
            if (!context.Blobs.Transitions.TryCreate(
                    effect.Blob,
                    context.Timeline,
                    out Tween animation))
            {
                return false;
            }

            context.Timeline.Join(animation);
            return true;
        }

        protected override bool PresentReverse(
            SpawnBlobEffect effect,
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

        protected override bool PresentReverseInBeat(
            SpawnBlobEffect effect,
            BoardEffectPresentationContext context)
        {
            if (!context.Blobs.Transitions.TryRemove(
                    effect.BlobId,
                    context.Timeline,
                    out Tween animation))
            {
                return false;
            }

            context.Timeline.Join(animation);
            return true;
        }
    }
}
