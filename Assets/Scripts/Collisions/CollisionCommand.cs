using System;

public class CollisionCommand : ICollisionCommand
{
    public static Action<BlobCollisionEventArgs> OnBlobCollision;
    public virtual void Execute(Blob blob, ICollidable other, BoardModel board)
    {
        OnBlobCollision?.Invoke(new BlobCollisionEventArgs(other));
        
    }

    

   
}