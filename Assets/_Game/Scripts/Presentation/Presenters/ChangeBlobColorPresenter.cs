using Blobs.Core;
using Unity.Multiplayer.PlayMode;

namespace Blobs.Presentation
{
    /// <summary>
    /// Presents authoritative size changes after merge contact as a grow-then-settle beat.
    /// Undo runs this phase first so the survivor returns to its prior size before restore.
    /// </summary>
    internal sealed class ChangeBlobColorPresentationHandler :
        BoardEffectPresentationHandler<ChangeBlobColorEffect>
    {
        public override BoardEffectPresentationPhase Phase => BoardEffectPresentationPhase.Aftermath;

        protected override bool PresentOrdered(
            ChangeBlobColorEffect effect,
            BoardEffectPresentationContext context)
        {
            return Present(effect.BlobId, effect.After, context);
        }

        protected override bool PresentInBeat(
            ChangeBlobColorEffect effect,
            BoardEffectPresentationContext context)
        {
            return PresentOrdered(effect, context);
        }

        protected override bool PresentReverse(
            ChangeBlobColorEffect effect,
            BoardEffectPresentationContext context)
        {
            return Present(effect.BlobId, effect.Before, context);
        }

        private static bool Present(
            string blobId,
            BlobColor color,
            BoardEffectPresentationContext context)
        {
            if (!context.Blobs.TryGetView(blobId, out BlobView view))
                return false;

            if (!context.IsAnimated)
            {
                view.ApplySkin(context.Board.GetBlob(blobId));
                return true;
            }


            return true;
        }
    }
}
