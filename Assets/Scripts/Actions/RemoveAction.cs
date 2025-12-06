/// <summary>
/// Command to remove a blob from the board.
/// </summary>
public class RemoveAction : IAction
{
    public Blob Blob { get; private set; }

    public RemoveAction( Blob blobToRemove)
    {
        Blob = blobToRemove;
    }

    public void Execute(BoardModel boardLogic) => boardLogic.RemoveBlob(Blob);
    public void Undo(BoardModel boardLogic) => boardLogic.PlaceBlob(Blob);
}
