using System;
using System.Collections.Generic;

namespace Blobs.Core
{
    /// <summary>
    /// Semantic kind of a single beat inside a resolved move timeline.
    /// </summary>
    public enum MoveStepKind
    {
        /// <summary>The mover crosses one empty tile (may include departure effects such as trail spawns).</summary>
        Traverse,

        /// <summary>The mover collides with an occupant and the collision plan resolves at that tile.</summary>
        Merge
    }

    /// <summary>
    /// One sequential beat of a move timeline. Effects within a step are logically
    /// simultaneous; steps play one after another. Presentation maps each step to a
    /// joined animation beat.
    /// </summary>
    public sealed class MoveStep
    {
        public MoveStep(MoveStepKind kind, IReadOnlyList<IBoardEffect> effects)
        {
            Kind = kind;
            Effects = effects ?? throw new ArgumentNullException(nameof(effects));
        }

        public MoveStepKind Kind { get; }

        /// <summary>
        /// Ordered effects for this beat. Order matters for board application
        /// (e.g. remove occupant before moving onto its tile) even though the
        /// beat is presented as one simultaneous moment.
        /// </summary>
        public IReadOnlyList<IBoardEffect> Effects { get; }
    }
}
