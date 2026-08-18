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



    /// <summary>
    /// Flag capture: requires matching color and a board containing only the mover and
    /// the flag. Consumes the mover; the flag stays in place.
    /// </summary>
    public sealed class RockMoveStrategy : IMoveStrategy
    {
        public MovePlan BuildPlan(MoveContext context)
        {
            // Compute the direction of the attempted move
            var direction = context.Target.Position - context.Source.Position;
            // Move back one cell from the blocked position in the opposite direction
            // No, you do not need to normalize the direction if you know that Target.Position - Source.Position is already a single step vector (i.e., the player can only move one cell at a time, so direction is already normalized).
            // If movement can span more than one cell in a single move (e.g. jumping multiple tiles), you would need to normalize. 
            var newPosition = context.Target.Position - GridPosition.Normalize(direction);


            return MovePlan.Continue(new Move(context.Source.Position, newPosition));
        }
    }


}
