using Blobs.Core;

namespace Blobs.Presentation
{
    /// <summary>
    /// Presents authoritative size changes after merge contact as a grow-then-settle beat.
    /// Undo runs this phase first so the survivor returns to its prior size before restore.
    /// </summary>
    internal sealed class ChangeBlobSizePresentationHandler :
        BoardEffectPresentationHandler<ChangeBlobSizeEffect>
    {
        public override BoardEffectPresentationPhase Phase => BoardEffectPresentationPhase.Aftermath;

        protected override bool PresentOrdered(
            ChangeBlobSizeEffect effect,
            BoardEffectPresentationContext context)
        {
            return Present(effect.BlobId, effect.After, context);
        }

        protected override bool PresentInBeat(
            ChangeBlobSizeEffect effect,
            BoardEffectPresentationContext context)
        {
            return PresentOrdered(effect, context);
        }

        protected override bool PresentReverse(
            ChangeBlobSizeEffect effect,
            BoardEffectPresentationContext context)
        {
            return Present(effect.BlobId, effect.Before, context);
        }

        private static bool Present(
            string blobId,
            BlobSize size,
            BoardEffectPresentationContext context)
        {
            if (!context.Blobs.TryGetView(blobId, out BlobView view))
                return false;

            if (!context.IsAnimated)
            {
                view.ApplySize(size);
                return true;
            }

            context.Timeline.Append(
                view.AnimateSize(size, context.Blobs.SizeChangeDuration));
            return true;
        }
    }
}
