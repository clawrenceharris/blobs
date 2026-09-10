#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// Run with the Blobs scene in Play Mode. Uses the same selection/animation path as input.
public static class PresentationSmokeCheck
{
    [MenuItem("Tools/Presentation/Run Lifecycle Smoke Check")]
    public static void Run()
    {
        if (!EditorApplication.isPlaying) throw new InvalidOperationException("Enter Play Mode in Blobs first.");
        var manager = UnityEngine.Object.FindFirstObjectByType<LevelManager>();
        var runner = new GameObject("Presentation Smoke Check").AddComponent<PresentationSmokeRunner>();
        runner.StartCoroutine(Check(manager, runner.gameObject));
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception("Presentation smoke check: " + message);
    }

    private static IEnumerator Check(LevelManager manager, GameObject runner)
    {
        var tutorial = UnityEngine.Object.FindFirstObjectByType<TutorialPresenter>();
        var board = manager.Board;
        var select = typeof(BoardPresenter).GetMethod("HandleBlobSelected", BindingFlags.NonPublic | BindingFlags.Instance);
        try
        {
            manager.StartLevel(1);
            yield return new WaitForSeconds(2);
            Require(BlobInput.InputEnabled, "level one input disabled");
            for (int level = 1; level <= 2; level++)
            {
                Require(manager.LevelNum == level, "automatic level order");
                int steps = manager.Level.tutorialSteps.Length;
                for (int step = 0; step < steps; step++)
                {
                    Require(tutorial.TutorialLogic != null, "tutorial missing");
                    var logic = tutorial.TutorialLogic;
                    var from = logic.StartBlob;
                    var to = logic.EndBlob;
                    Require(from != null && to != null, "tutorial targets missing");
                    select.Invoke(board, new object[] { from });
                    select.Invoke(board, new object[] { to });
                    float deadline = Time.realtimeSinceStartup + 20;
                    yield return null;
                    while (!BlobInput.InputEnabled && Time.realtimeSinceStartup < deadline) yield return null;
                    Require(BlobInput.InputEnabled, "merge/transition timed out");
                    yield return new WaitForSeconds(1);
                }
                Require(manager.LevelNum == level + 1, "completion did not advance");
            }
            Require(tutorial.TutorialLogic == null, "non-tutorial level retained tutorial");
            Require(ActionInvoker._actions.Count == 0, "previous level undo history retained");
            for (int level = 1; level <= 7; level++)
            {
                manager.StartLevel(level);
                yield return new WaitForSeconds(2);
                foreach (var view in board.TileViews.Values)
                {
                    var point = Camera.main.WorldToViewportPoint(view.transform.position);
                    Require(point.x > 0 && point.x < 1 && point.y > 0 && point.y < 1, "board clipped by camera");
                }
                var previousViews = board.BlobViews.Values.Select(v => v.gameObject).ToArray();
                manager.Restart();
                yield return new WaitForSeconds(2);
                Require(manager.LevelNum == level && manager.Level.levelNum == level, "level identity mismatch after restart");
                Require(previousViews.All(v => v == null), "restart retained old blob objects");
                Require(board.BlobViews.Count == manager.Level.blobs.Length, "blob count mismatch");
                Require(board.TileViews.Count == manager.Level.tiles.Length, "tile count mismatch");
                Require(manager.IsTutorial == (tutorial.TutorialLogic != null), "tutorial lifecycle mismatch");
                if (manager.IsTutorial)
                    Require(tutorial.TutorialLogic.CurrentStep == manager.Level.tutorialSteps[0], "restart did not reset tutorial");
                Require(UnityEngine.Object.FindObjectsByType<BlobView>(FindObjectsSortMode.None).Length == board.BlobViews.Count, "orphan blob views");
                Require(UnityEngine.Object.FindObjectsByType<TileView>(FindObjectsSortMode.None).Length == board.TileViews.Count, "orphan tile views");
            }
            // Exercise the final animation and next-level boundary without a level-eight file.
            tutorial.ResetTutorial();
            BlobInput.DisableInput();
            yield return board.CompleteMergeCo(board.BoardLogic.GetAllBlobs().First(), manager.StartNextLevel);
            yield return new WaitForSeconds(2);
            Require(board.BoardLogic == null && board.BlobViews.Count == 0 && board.TileViews.Count == 0, "last-level cleanup");
            Require(!BlobInput.InputEnabled, "last-level input still enabled");
            Require(!LevelLoader.HasLevel(8) && LevelLoader.LoadLevelData(8) == null, "missing level boundary");
            Debug.Log("PRESENTATION SMOKE PASS: actual tutorial merges 1→2→3; restart and object cleanup 1–7; tutorial/free-play reset; final animation and level-eight boundary.");
        }
        finally
        {
            UnityEngine.Object.Destroy(runner);
        }
    }
}
public class PresentationSmokeRunner : MonoBehaviour { }
#endif
