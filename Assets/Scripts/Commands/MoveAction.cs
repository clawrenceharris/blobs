using System;
using System.Collections;
using Blobs.Commands;
using UnityEngine;

/// <summary>
/// Command to move a blob from one position to another.
/// </summary>
public class MoveAction : IAction
{
    public Blob Blob { get; set; }
    public Vector2Int StartPosition { get; private set; }
    public Vector2Int EndPosition { get; private set; }
    public MoveAction(Blob blob, Vector2Int startPosition, Vector2Int endPosition)
    {
        Blob = blob;
        StartPosition = startPosition;
        EndPosition = endPosition;
    }

    public void Execute(CommandManager context)
    {
        context.Board.MoveBlob(Blob.ID, EndPosition);
    }
    public void Undo(CommandManager context)
    {
        context.Board.MoveBlob(Blob.ID, StartPosition);
    }

    
}