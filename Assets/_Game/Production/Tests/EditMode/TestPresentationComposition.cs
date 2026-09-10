using Blobs.Presentation;
using Blobs.Content;
using UnityEditor;
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
            ConfigureSurface(surfaceObject.AddComponent<BoardSurfaceView>());

            var presenter = root.AddComponent<BoardPresenter>();
            var serialized = new SerializedObject(root.GetComponent<BlobPresenter>());
            serialized.FindProperty("blobAnimationSettings").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<BlobAnimationSettingsAsset>(
                    "Assets/_Game/Production/Content/Presentation/BlobAnimationSettings.asset");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return presenter;
        }
        public static void ConfigureSurface(BoardSurfaceView view)
        {
            var serialized = new SerializedObject(view);
            serialized.FindProperty("spriteSet").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<BoardSurfaceSpriteSet>(
                    "Assets/_Game/Production/Resources/BoardSurfaceSpriteSet.asset");
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
