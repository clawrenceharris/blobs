using System;

public class BlobCollisionEventArgs : EventArgs
{
    public ICollidable Other { get; }

    public BlobCollisionEventArgs(ICollidable other)
    {
        Other = other;
    }
}