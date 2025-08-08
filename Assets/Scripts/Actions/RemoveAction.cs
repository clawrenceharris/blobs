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

    public void Execute(BoardLogic boardLogic) => boardLogic.RemoveBlob(Blob);
    public void Undo(BoardLogic boardLogic) => boardLogic.PlaceBlob(Blob);
}
