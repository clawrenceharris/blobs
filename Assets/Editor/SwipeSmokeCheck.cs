#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class SwipeSmokeCheck
{
    [MenuItem("Tools/Presentation/Run Legacy Swipe Smoke Check")]
    public static void Run()
    {
        if (!EditorApplication.isPlaying) throw new InvalidOperationException("Enter Play Mode in Blobs first.");
        var runner = new GameObject("Swipe Smoke Check").AddComponent<PresentationSmokeRunner>();
        runner.StartCoroutine(Check(runner.gameObject));
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception("Swipe smoke check: " + message);
    }

    private static void Pointer(BlobInput input, string method, params object[] arguments)
    {
        typeof(BlobInput).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(input, arguments);
    }

    private static IEnumerator Check(GameObject runner)
    {
        var manager = UnityEngine.Object.FindFirstObjectByType<LevelManager>();
        var tutorial = UnityEngine.Object.FindFirstObjectByType<TutorialPresenter>();
        try
        {
            manager.StartLevel(1);
            yield return new WaitForSeconds(2);
            var source = tutorial.TutorialLogic.StartBlob;
            var input = manager.Board.GetBlobView(source.ID).Input;
            Vector2 start = Camera.main.WorldToScreenPoint((Vector2)source.GridPosition * BoardPresenter.TileSize);
            Vector2 target = Camera.main.WorldToScreenPoint((Vector2)tutorial.TutorialLogic.EndBlob.GridPosition * BoardPresenter.TileSize);
            int initial = manager.Board.BoardLogic.GetAllBlobs().Count;

            Pointer(input, "BeginPointer", start, -1);
            BlobInput.DisableInput();
            BlobInput.EnableInput();
            Pointer(input, "EndPointer", target);
            Require(manager.Board.BoardLogic.GetAllBlobs().Count == initial, "canceled gesture merged");

            Pointer(input, "BeginPointer", start, -1);
            Pointer(input, "EndPointer", Vector2.Lerp(start, target, 0.5f));
            Require(manager.Board.BoardLogic.GetAllBlobs().Count == initial, "empty endpoint merged");

            // Sub-threshold motion stays a tap; a later swipe must replace that selection.
            Pointer(input, "BeginPointer", start, -1);
            Pointer(input, "EndPointer", start + Vector2.one);
            Require(manager.Board.BoardLogic.GetAllBlobs().Count == initial, "tap merged on its own");
            Pointer(input, "BeginPointer", start, -1);
            Pointer(input, "EndPointer", target);
            Require(!BlobInput.InputEnabled, "swipe did not start merge / lock input");
            yield return new WaitForSeconds(4);
            Require(tutorial.TutorialLogic.CurrentStep == manager.Level.tutorialSteps[1], "horizontal swipe did not advance tutorial exactly once");

            source = tutorial.TutorialLogic.StartBlob;
            input = manager.Board.GetBlobView(source.ID).Input;
            start = Camera.main.WorldToScreenPoint((Vector2)source.GridPosition * BoardPresenter.TileSize);
            target = Camera.main.WorldToScreenPoint((Vector2)tutorial.TutorialLogic.EndBlob.GridPosition * BoardPresenter.TileSize);
            // Same gesture processing used by a tracked touch finger.
            Pointer(input, "BeginPointer", start, 42);
            Pointer(input, "EndPointer", target);
            yield return new WaitForSeconds(6);
            Require(manager.LevelNum == 2, "vertical swipe did not complete level one");
            Require(tutorial.TutorialLogic.CurrentStep == manager.Level.tutorialSteps[0], "new level tutorial did not reset");
            Debug.Log("LEGACY SWIPE SMOKE PASS: canceled press, empty endpoint, tap tolerance, press/release-target merges, input lock, and next-level tutorial reset.");
        }
        finally
        {
            UnityEngine.Object.Destroy(runner);
        }
    }
}
#endif
