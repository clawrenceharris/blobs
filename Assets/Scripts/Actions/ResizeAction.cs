using System;

public class ResizeAction : IAction
{
    public Blob Blob { get; private set; }
    private readonly int _direction;
    public ResizeAction(Blob blob, int direction)
    {
        Blob = blob;
        _direction = direction;
    }
    public void Execute(BoardModel board)
    {
        
        Blob.Size = Blob.Size + _direction;

    }

    public void Undo(BoardModel board)
    {
        Blob.Size = Blob.Size - _direction;
    }
}