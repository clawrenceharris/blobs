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
    public bool CheckForWin(BoardModel board)
    {
        // There must be no clearable Blobs on the board to win.
        var clearableBlobs = board.GetAllBlobs().OfType<IClearable>();
        if (clearableBlobs.Count() == 0)
        {
            return true;
        }
        return false;

    }
}