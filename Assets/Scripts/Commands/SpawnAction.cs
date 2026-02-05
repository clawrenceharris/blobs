using System.Collections;
using System.Collections.Generic;
using Blobs.Commands;
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

    public void Execute(CommandManager context)
    {
        context.Board.PlaceBlob(Blob);

    }
    public void Undo(CommandManager context)
    {
        context.Board.RemoveBlob(Blob.ID);

    }

}
