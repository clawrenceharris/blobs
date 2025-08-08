using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Command to remove a blob from the board.
/// </summary>
public class SpawnAction : IAction
{
    public  Blob Blob { get; private set; }
    public SpawnAction(Blob blob)
    {
        Blob = blob;
    }

    public void Execute(BoardLogic board)
    {
        board.PlaceBlob(Blob);

    }
    public void Undo(BoardLogic board)
    {
        board.RemoveBlob(Blob);

    }

}
