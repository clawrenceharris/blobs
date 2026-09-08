using Blobs.Core;

namespace Blobs.Presentation
{
    /// <summary>
    /// Dispatches a complete merge as one travel beat. Normal/reverse outcomes are carried
    /// by MergeEffect, so flat lists and grouped steps use exactly the same choreography.
    /// </summary>
    internal sealed class MergePresentationHandler : BoardEffectPresentationHandler<MergeEffect>
    {
        public override BoardEffectPresentationPhase Phase => BoardEffectPresentationPhase.Travel;
        public override bool RepresentsMovement => true;

        protected override bool PresentOrdered(MergeEffect effect, BoardEffectPresentationContext context)
            => context.Blobs.Merges.Present(effect, context.Timeline, context.ContactFeedback);

        protected override bool PresentInBeat(MergeEffect effect, BoardEffectPresentationContext context)
            => PresentOrdered(effect, context);
    }
}
