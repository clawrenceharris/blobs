using System.Collections;
using Blobs.Commands;


/// <summary>
/// Command to remove a blob from the board.
/// </summary>
public class RemoveAction : IAction
{
    public Blob Blob { get; set; }

    public RemoveAction( Blob blobToRemove)
    {
        Blob = blobToRemove;
    }

    public void Execute(CommandManager context) => context.Board.RemoveBlob(Blob);
    public void Undo(CommandManager context) => context.Board.PlaceBlob(Blob);
}
