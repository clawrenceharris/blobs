using Blobs.Core;
using DG.Tweening;

namespace Blobs.Presentation
{
    internal sealed class GhostReturnPresentationHandler : BoardEffectPresentationHandler<GhostReturnEffect>
    {
        public override BoardEffectPresentationPhase Phase => BoardEffectPresentationPhase.Travel;
        public override bool RepresentsMovement => true;
        protected override bool PresentOrdered(GhostReturnEffect effect,
            BoardEffectPresentationContext context) => context.Blobs.GhostReturns.Present(
                effect, context.Timeline, context.ContactFeedback);
        protected override bool PresentInBeat(GhostReturnEffect effect,
            BoardEffectPresentationContext context) => PresentOrdered(effect, context);
    }
}
