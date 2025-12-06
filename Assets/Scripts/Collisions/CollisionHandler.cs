

public class CollisionHandler<T> where T : ICollisionCommand, new()
{
    public T collidable;
    public void HandleCollision(Blob player, ICollidable other, BoardModel board)
    {

        collidable = new();
        collidable.Execute(player, other, board);

    }

     

}