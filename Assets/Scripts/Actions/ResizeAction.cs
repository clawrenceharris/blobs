using System;

public class ResizeAction : IAction
{
    public Blob Blob { get; private set; }
    private int _direction;
    public ResizeAction(Blob blob, int direction)
    {
        Blob = blob;
        _direction = direction;
    }
    public void Execute(BoardLogic board)
    {
        
        Blob.Size = Blob.Size + _direction;

    }

    public void Undo(BoardLogic board)
    {
        Blob.Size = Blob.Size - _direction;
    }
}