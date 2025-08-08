
public interface ICollisionCommand
{
    void Execute(Blob player, ICollidable other, BoardLogic board);

}