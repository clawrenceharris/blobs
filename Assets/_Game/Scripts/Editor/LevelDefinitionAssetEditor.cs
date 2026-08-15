using Blobs.Content;
using UnityEditor;
using UnityEngine;

namespace Blobs.Editor
{
    [CustomEditor(typeof(LevelDefinitionAsset))]
    public sealed class LevelDefinitionAssetEditor : UnityEditor.Editor
    {
        private SerializedProperty _tiles;
        private SerializedProperty _blobs;

        private void OnEnable()
        {
            _tiles = serializedObject.FindProperty("tiles");
            _blobs = serializedObject.FindProperty("blobs");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Draw all properties except m_Script, blobs, and tiles
            DrawPropertiesExcluding(serializedObject, "m_Script", "tiles", "blobs");

            EditorGUILayout.Space();

            // --- Tiles Section ---
            EditorGUILayout.LabelField("Tiles", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_tiles, includeChildren: true);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Add Normal Tile"))
                    AddTile(new NormalTileAssetData());
            }

            EditorGUILayout.Space();

            // --- Blobs Section ---
            EditorGUILayout.LabelField("Blobs", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_blobs, includeChildren: true);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Add Normal Blob"))
                    AddBlob(new NormalBlobAssetData());

                if (GUILayout.Button("Add Flag Blob"))
                    AddBlob(new FlagBlobAssetData());
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void AddTile(TileAssetData tile)
        {
            _tiles.InsertArrayElementAtIndex(_tiles.arraySize);
            SerializedProperty element = _tiles.GetArrayElementAtIndex(_tiles.arraySize - 1);
            element.managedReferenceValue = tile;
        }

        private void AddBlob(BlobAssetData blob)
        {
            _blobs.InsertArrayElementAtIndex(_blobs.arraySize);
            SerializedProperty element = _blobs.GetArrayElementAtIndex(_blobs.arraySize - 1);
            element.managedReferenceValue = blob;
        }
    }
}
