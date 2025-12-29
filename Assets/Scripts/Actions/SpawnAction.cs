using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Command to spawn a blob on the board.
/// </summary>
public class SpawnAction : IAction
{
    public Blob Blob { get; set; }
    private Vector2Int _at;

    public SpawnAction(Blob blob)
    {
        Blob = blob;
    }

    public void Execute(BoardModel board)
    {
        board.PlaceBlob(Blob);

    }
    public void Undo(BoardModel board)
    {
        board.RemoveBlob(Blob.ID);

    }

}
