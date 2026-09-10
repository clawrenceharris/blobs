using System.Collections.Generic;

namespace Blobs.Core
{
    /// <summary>
    /// Resolves what happens when a moving blob collides with an occupant on its path.
    /// Strategies emit collision-tile effects only; the resolver owns locomotion, so the
    /// same strategy works for intermediate chain merges and for the final target.
    /// </summary>
    public interface IMoveStrategy
    {
        MovePlan BuildPlan(MoveContext context);
    }


    public sealed class NormalMoveStrategy : IMoveStrategy
    {
        public MovePlan BuildPlan(MoveContext context)
        {
            return MovePlan.Move(context.Source, context.Target);
        }
    }


}
