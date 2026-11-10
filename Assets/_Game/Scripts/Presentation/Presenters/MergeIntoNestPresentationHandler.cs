using Blobs.Core;

namespace Blobs.Presentation
{
    /// <summary>
    /// Dispatches nest capture as one travel beat. Occupant bookkeeping lives on the
    /// effect itself, so presentation does not need a separate AddOccupant handler.
    /// </summary>
    internal sealed class MergeIntoNestPresentationHandler
        : BoardEffectPresentationHandler<MergeIntoNestEffect>
    {
        public override BoardEffectPresentationPhase Phase => BoardEffectPresentationPhase.Travel;
        public override bool RepresentsMovement => true;

        protected override bool PresentOrdered(
            MergeIntoNestEffect effect,
            BoardEffectPresentationContext context)
            => context.Blobs.Nests.Present(effect, context.Timeline, context.ContactFeedback);

        protected override bool PresentInBeat(
            MergeIntoNestEffect effect,
            BoardEffectPresentationContext context)
            => PresentOrdered(effect, context);

        protected override bool PresentReverse(
            MergeIntoNestEffect effect,
            BoardEffectPresentationContext context)
            => context.Blobs.Nests.PresentReverse(effect, context);

        protected override bool PresentReverseInBeat(
            MergeIntoNestEffect effect,
            BoardEffectPresentationContext context)
            => PresentReverse(effect, context);
    }
}
