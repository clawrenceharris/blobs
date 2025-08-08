
public interface ICollidable
{
    void HandleCollision(Blob player, ICollidable other, BoardLogic board);
}