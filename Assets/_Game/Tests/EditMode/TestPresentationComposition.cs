using Blobs.Presentation;
using UnityEngine;

namespace Blobs.Tests.EditMode
{
    /// <summary>
    /// Creates the authored-equivalent presentation hierarchy required by board presenter tests.
    /// </summary>
    internal static class TestPresentationComposition
    {
        public static BoardPresenter AddBoardPresenter(GameObject root)
        {
            root.AddComponent<MergeAnimationOrchestrator>();

            var surfaceObject = new GameObject("Board Surface");
            surfaceObject.transform.SetParent(root.transform, false);
            surfaceObject.AddComponent<BoardSurfaceView>();

            return root.AddComponent<BoardPresenter>();
        }
    }
}
