using Blobs.Core;
using DG.Tweening;

namespace Blobs.Presentation
{
    internal sealed class GhostHauntPresentationHandler : BoardEffectPresentationHandler<GhostHauntEffect>
    {
        public override BoardEffectPresentationPhase Phase => BoardEffectPresentationPhase.Travel;
        public override bool RepresentsMovement => true;
        protected override bool PresentOrdered(GhostHauntEffect effect,
            BoardEffectPresentationContext context) => context.Blobs.GhostReturns.Present(
                effect, context.Timeline, context.ContactFeedback);
        protected override bool PresentInBeat(GhostHauntEffect effect,
            BoardEffectPresentationContext context) => PresentOrdered(effect, context);
    }
}
