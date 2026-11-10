using Blobs.Content;
using Blobs.Core;
using UnityEditor;
using UnityEngine;

namespace Blobs.Editor
{
    [CustomEditor(typeof(LevelDefinitionAsset))]
    public sealed class LevelDefinitionAssetEditor : UnityEditor.Editor
    {
        private SerializedProperty _tiles;
        private SerializedProperty _blobs;
        private SerializedProperty _objectiveItems;

        private void OnEnable()
        {
            _tiles = serializedObject.FindProperty("tiles");
            _blobs = serializedObject.FindProperty("blobs");
            _objectiveItems = serializedObject.FindProperty("objective").FindPropertyRelative("items");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Draw all properties except m_Script, blobs, tiles, and objective
            DrawPropertiesExcluding(serializedObject, "m_Script", "tiles", "blobs", "objective");

            EditorGUILayout.Space();

            // --- Tiles Section ---
            EditorGUILayout.LabelField("Tiles", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_tiles, includeChildren: true);

            using (new EditorGUILayout.HorizontalScope())
            {

                if (GUILayout.Button("Add Grave Tile"))
                {
                    AddTile(new GraveTileAssetData()
                    {
                        id = $"tile-{_tiles.arraySize - 1}",
                        type = TileType.Grave,
                    });
                }
            }

            EditorGUILayout.Space();

            // --- Blobs Section ---
            EditorGUILayout.LabelField("Blobs", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_blobs, includeChildren: true);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Add Normal Blob"))
                    AddBlob(new NormalBlobAssetData()
                    {
                        id = $"blob-{_blobs.arraySize}",
                        type = BlobType.Normal,
                    });

                if (GUILayout.Button("Add Nest"))
                    AddBlob(new NestBlobAssetData()
                    {
                        id = $"blob-{_blobs.arraySize}",
                        type = BlobType.Nest,
                    });
                if (GUILayout.Button("Add Trail Blob"))
                    AddBlob(new TrailBlobAssetData()
                    {
                        id = $"blob-{_blobs.arraySize}",
                        type = BlobType.Trail,
                    });
                if (GUILayout.Button("Add Rock Blob"))
                    AddBlob(new RockBlobAssetData()
                    {
                        id = $"blob-{_blobs.arraySize}",
                        type = BlobType.Rock,
                    });
                if (GUILayout.Button("Add Ghost Blob"))
                    AddBlob(new GhostBlobAssetData()
                    {
                        id = $"blob-{_blobs.arraySize}",
                        type = BlobType.Ghost,
                    });
            }

            EditorGUILayout.Space();

            // --- Objective Section ---
            EditorGUILayout.LabelField("Objective", EditorStyles.boldLabel);
            if (_objectiveItems.arraySize == 0)
                EditorGUILayout.HelpBox("No items: the level is won by clearing every clearable blob.", MessageType.Info);
            EditorGUILayout.PropertyField(_objectiveItems, new GUIContent("Items"), includeChildren: true);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Add Blob Objective"))
                    AddObjective(new BlobObjectiveTargetAssetData { type = BlobType.Normal });
                if (GUILayout.Button("Add Nest Objective"))
                    AddObjective(new BlobObjectiveTargetAssetData { type = BlobType.Nest });
                if (GUILayout.Button("Add Tile Objective"))
                    AddObjective(new TileObjectiveTargetAssetData { type = TileType.Grave });
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

        private void AddObjective(ObjectiveTargetAssetData target)
        {
            _objectiveItems.InsertArrayElementAtIndex(_objectiveItems.arraySize);
            SerializedProperty element = _objectiveItems.GetArrayElementAtIndex(_objectiveItems.arraySize - 1);
            // The inserted element copies the previous one, including its managed reference.
            element.FindPropertyRelative("target").managedReferenceValue = target;
            element.FindPropertyRelative("amount").intValue = 1;
        }
    }
}
