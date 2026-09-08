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

}
