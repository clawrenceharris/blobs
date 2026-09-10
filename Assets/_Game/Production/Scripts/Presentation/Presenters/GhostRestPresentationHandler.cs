using Blobs.Core;

namespace Blobs.Presentation
{
    internal sealed class GhostRestPresentationHandler : BoardEffectPresentationHandler<GhostRestEffect>
    {
        public override BoardEffectPresentationPhase Phase => BoardEffectPresentationPhase.Travel;
        public override bool RepresentsMovement => true;
        protected override bool PresentOrdered(GhostRestEffect effect,
            BoardEffectPresentationContext context) => context.Blobs.GhostRests.Present(
                effect, context.Timeline, context.ContactFeedback);
        protected override bool PresentInBeat(GhostRestEffect effect,
            BoardEffectPresentationContext context) => PresentOrdered(effect, context);
    }
}
