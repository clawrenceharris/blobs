using System;
using System.Collections;
using Blobs.Commands;

public class ResizeAction : IAction
{
    public Blob Blob { get; set; }
    private readonly int _direction;

    public ResizeAction(Blob blob, int direction)
    {
        Blob = blob;
        _direction = direction;
    }
    public void Execute(CommandManager context)
    {
        
        Blob.Size = Blob.Size + _direction;

    }

    public void Undo(CommandManager context)
    {
        Blob.Size = Blob.Size - _direction;
    }
}