/// <summary>
/// A rule that requires the target and the compared object to be on the same row or column on the board.
/// </summary>
public class PositionRule : IRule<IBoardElement>
{
    public bool Validate(IBoardElement target, IBoardElement other, BoardLogic board)
    {
        return target.GridPosition.x == other.GridPosition.x || target.GridPosition.y == other.GridPosition.y;
    }
}