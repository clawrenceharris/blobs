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


            var presenter = root.AddComponent<BoardPresenter>();
            var serialized = new SerializedObject(root.GetComponent<BlobPresenter>());
            serialized.FindProperty("blobAnimationSettings").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<BlobAnimationSettingsAsset>(
                    "Assets/_Game/Content/Presentation/BlobAnimationSettings_Default.asset");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return presenter;
        }
    }
}
