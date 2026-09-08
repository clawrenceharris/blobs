using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor menu to validate board integrity (e.g. after merge/undo). Use when testing multi-merge + undo.
/// </summary>
public static class BoardIntegrityValidatorMenu
{
    [MenuItem("Blobs/Validate board integrity")]
    public static void ValidateCurrentBoard()
    {
        var presenter = Object.FindFirstObjectByType<BoardPresenter>();
        if (presenter == null)
        {
            Debug.LogWarning("[BoardIntegrityValidator] No BoardPresenter in scene. Run during play mode with a level loaded.");
            return;
        }

        bool ok = presenter.ValidateBoardIntegrity("Editor menu");
        Debug.Log(ok ? "[BoardIntegrityValidator] Board integrity OK." : "[BoardIntegrityValidator] See errors above.");
    }
}
