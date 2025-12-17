using System;
using System.Collections;

public class ResizeAction : IAction
{
    public Blob Blob { get; set; }
    private readonly int _direction;
    public IEnumerator Animate(BoardPresenter presenter) => null;

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