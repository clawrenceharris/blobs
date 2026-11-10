using System.Collections.Generic;

namespace Blobs.Core
{
    /// <summary>
    /// Shapes the selected source/target intent before the resolver walks the path.
    /// Use a move strategy for pair-level validation or rare whole-route changes that
    /// must be known before per-tile simulation begins, such as nest eligibility or a
    /// mechanic that changes the starting point, ending point, source, or intended target.
    /// </summary>
    /// <remarks>
    /// Move strategies are not the normal place for occupant contact rules. If behavior
    /// depends on what the mover hits while traversing the path, use an
    /// <see cref="ICollisionStrategy"/> instead so intermediate and final occupants are
    /// handled consistently by the resolver.
    /// </summary>
    public interface IMoveStrategy
    {
        /// <summary>
        /// Builds the initial move plan from the selected source and intended target.
        /// The returned plan defines the path the resolver will simulate one tile at a
        /// time; collision behavior still belongs to collision strategies encountered
        /// during that simulation.
        /// </summary>
        MovePlan BuildPlan(MoveContext context);
    }

    /// <summary>
    /// Default move planning: preserve the selected source and target and let the
    /// resolver walk the direct aligned path between them.
    /// </summary>
    public sealed class NormalMoveStrategy : IMoveStrategy
    {
        public MovePlan BuildPlan(MoveContext context)
        {
            return context.Plan.Success(context.Source, context.Target);
        }
    }

    /// <summary>
    /// Pair-level gate for nest merge intents. The actual merge is still resolved
    /// by <see cref="NestCollisionStrategy"/> when the mover reaches the nest cell.
    /// </summary>
    public sealed class NestMoveStrategy : IMoveStrategy
    {
        public MovePlan BuildPlan(MoveContext context)
        {
            var source = context.Source;
            var nest = context.Target;

            if (source.Type != BlobType.Normal)
                return context.Plan.Failed(MoveFailureReason.NestRequiresNormalSource);
            if (source.Components.Color?.Color != nest.Components.Color?.Color)
                return context.Plan.Failed(MoveFailureReason.NestRequiresMatchingColor);
            if (nest.HasOccupant)
                return context.Plan.Failed(MoveFailureReason.NestAlreadyOccupied);
            return context.Plan.Success(source, nest);

        }
    }


}
