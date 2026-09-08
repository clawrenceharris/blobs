using Blobs.Core;
using DG.Tweening;

namespace Blobs.Presentation
{
    internal sealed class ClearGhostPresentationHandler : BoardEffectPresentationHandler<ClearGhostEffect>
    {
        public override BoardEffectPresentationPhase Phase => BoardEffectPresentationPhase.Aftermath;
        protected override bool PresentOrdered(ClearGhostEffect effect,
            BoardEffectPresentationContext context)
        {
            if (!context.Blobs.TryRetireView(effect.GhostId, out BlobView ghost)) return false;
            if (!context.IsAnimated)
            {
                context.Blobs.DestroyRetiringView(ghost);
                return true;
            }
            context.Timeline.AppendAsync(async token =>
            {
                // The visible halo contracts at the Sigil after return travel has completed.
                await PresentationTimeline.AwaitTweenAsync(ghost.PlayDespawn(0.2f), token);
                context.Blobs.DestroyRetiringView(ghost);
            });
            return true;
        }
        protected override bool PresentInBeat(ClearGhostEffect effect,
            BoardEffectPresentationContext context) => PresentOrdered(effect, context);
    }
}
