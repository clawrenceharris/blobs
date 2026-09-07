using System;
using System.Collections.Generic;
using Blobs.Core;

namespace Blobs.Presentation
{
    /// <summary>
    /// Presents a semantic move step whose choreography cannot be expressed as independent effects.
    /// </summary>
    public interface IMoveStepPresentationHandler
    {
        /// <summary>
        /// Whether a handled step establishes an arrival point and therefore owns final-move feedback.
        /// </summary>
        bool RepresentsMovement { get; }

        /// <summary>Returns whether this handler understands the supplied step.</summary>
        bool CanPresent(MoveStep step);

        /// <summary>
        /// Presents the composite interaction and returns the exact effect instances it consumed.
        /// Unconsumed effects are still presented by their registered effect handlers.
        /// </summary>
        bool Present(
            MoveStep step,
            BoardEffectPresentationContext context,
            out IReadOnlyList<IBoardEffect> handledEffects);
    }

    /// <summary>
    /// Optional compatibility contract for composite interactions received as a flattened effect list.
    /// New resolution code should prefer semantic <see cref="MoveStep"/> instances.
    /// </summary>
    internal interface IOrderedEffectSequencePresentationHandler
    {
        bool CanPresent(IReadOnlyList<IBoardEffect> effects, int startIndex);

        bool Present(
            IReadOnlyList<IBoardEffect> effects,
            int startIndex,
            BoardEffectPresentationContext context,
            out int handledEffectCount);
    }

    /// <summary>
    /// Recognizes and presents the coordinated removal and movement of a normal merge.
    /// </summary>
    internal sealed class NormalMergeStepPresentationHandler :
        IMoveStepPresentationHandler,
        IOrderedEffectSequencePresentationHandler
    {
        public bool RepresentsMovement => true;

        public bool CanPresent(MoveStep step)
        {
            return step != null &&
                step.Kind == MoveStepKind.Merge &&
                TryMatch(step.Effects, out _, out _);
        }

        public bool Present(
            MoveStep step,
            BoardEffectPresentationContext context,
            out IReadOnlyList<IBoardEffect> handledEffects)
        {
            handledEffects = Array.Empty<IBoardEffect>();
            if (step == null ||
                step.Kind != MoveStepKind.Merge ||
                !TryMatch(step.Effects, out MoveBlobEffect source, out RemoveBlobEffect target))
            {
                return false;
            }

            if (!context.Blobs.NormalMerges.Present(
                    source,
                    target,
                    context.Timeline,
                    context.ContactFeedback))
            {
                return false;
            }

            handledEffects = new IBoardEffect[] { source, target };
            return true;
        }

        bool IOrderedEffectSequencePresentationHandler.CanPresent(
            IReadOnlyList<IBoardEffect> effects,
            int startIndex)
        {
            return TryMatchOrdered(effects, startIndex, out _, out _);
        }

        bool IOrderedEffectSequencePresentationHandler.Present(
            IReadOnlyList<IBoardEffect> effects,
            int startIndex,
            BoardEffectPresentationContext context,
            out int handledEffectCount)
        {
            handledEffectCount = 0;
            if (!TryMatchOrdered(
                    effects,
                    startIndex,
                    out MoveBlobEffect source,
                    out RemoveBlobEffect target))
            {
                return false;
            }

            if (!context.Blobs.NormalMerges.Present(source, target, context.Timeline))
                return false;

            handledEffectCount = 2;
            return true;
        }

        private static bool TryMatch(
            IReadOnlyList<IBoardEffect> effects,
            out MoveBlobEffect source,
            out RemoveBlobEffect target)
        {
            source = null;
            target = null;

            foreach (IBoardEffect effect in effects)
            {
                if (effect is MoveBlobEffect candidateSource)
                {
                    source = candidateSource;
                    break;
                }
            }

            if (source == null)
                return false;

            foreach (IBoardEffect effect in effects)
            {
                if (effect is RemoveBlobEffect candidateTarget &&
                    candidateTarget.BlobId != source.BlobId &&
                    candidateTarget.At == source.To)
                {
                    target = candidateTarget;
                    return true;
                }
            }

            return false;
        }

        private static bool TryMatchOrdered(
            IReadOnlyList<IBoardEffect> effects,
            int startIndex,
            out MoveBlobEffect source,
            out RemoveBlobEffect target)
        {
            source = null;
            target = null;
            if (effects == null ||
                startIndex < 0 ||
                startIndex + 1 >= effects.Count ||
                effects[startIndex] is not RemoveBlobEffect candidateTarget ||
                effects[startIndex + 1] is not MoveBlobEffect candidateSource ||
                candidateSource.To != candidateTarget.At)
            {
                return false;
            }

            source = candidateSource;
            target = candidateTarget;
            return true;
        }
    }
}
