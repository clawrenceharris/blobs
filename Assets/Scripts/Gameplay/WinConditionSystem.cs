using System.Linq;
using UnityEngine;


/// <summary>
/// Encapsulates all logic for checking win conditions.
/// It is completely decoupled from the view and animations.
/// </summary>
public class WinConditionSystem
{
    /// <summary>
    /// Checks the board state for a win.
    /// </summary>
    /// <returns>The ID of the winning blob if conditions are met, otherwise null.</returns>
    public Blob CheckForWin(BoardLogic board)
    {
        // Win Condition: There must be exactly one blob on the board that is clearable.
        // This blob will be the winning blob.
        var clearableBlobs = board.GetAllBlobs().OfType<IClearable>();
        if (clearableBlobs.Count() == 1)
        {
            return (Blob)clearableBlobs.First();
        }
        return null;

    }
}