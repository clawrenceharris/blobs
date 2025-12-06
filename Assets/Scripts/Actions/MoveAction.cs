using System;
using UnityEngine;

/// <summary>
/// Command to move a blob from one position to another.
/// </summary>
public class MoveAction : IAction
{
    public Blob Blob { get; private set; }
    public Vector2Int FromPosition { get; private set; }
    public Vector2Int ToPosition { get; private set; }

    public MoveAction(Blob blob, Vector2Int fromPosition, Vector2Int toPosition)
    {
        Blob = blob;
        FromPosition = fromPosition;
        ToPosition = toPosition;
    }

    public void Execute(BoardModel board)
    {
        board.MoveBlob(Blob, ToPosition);
    }
    public void Undo(BoardModel board)
    {

        board.MoveBlob(Blob, FromPosition);
    }
}