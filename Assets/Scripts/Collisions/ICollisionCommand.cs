
public interface ICollisionCommand
{
    void Execute(Blob player, ICollidable other, BoardModel board);

}