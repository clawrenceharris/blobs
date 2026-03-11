using System;
using UnityEngine;
using UnityEditor;

namespace Blobs.Editor
{
    [CustomEditor(typeof(LevelData))]
    public class LevelDataEditor : UnityEditor.Editor
    {
        #region Constants
        
        private const float CELL_SIZE = 40f;
        private const float CELL_SPACING = 2f;
        private const float SPACING_SMALL = 5f;
        private const float SPACING_MEDIUM = 10f;
        private const float SPACING_LARGE = 15f;
        private const float FIELD_WIDTH_SMALL = 80f;
        private const float FIELD_WIDTH_MEDIUM = 150f;
        private const float FIELD_WIDTH_LARGE = 250f;
        private const float BUTTON_HEIGHT = 22f;
        private const float GRID_MAX_HEIGHT = 400f;
        
        #endregion

        #region Style Configuration
        
        private static class EditorStyleConfig
        {
            // Selection Colors (replacing vomit green)
            public static readonly Color SelectionActive = new Color(0.2f, 0.6f, 0.9f); // Professional blue
            public static readonly Color SelectionHover = new Color(0.3f, 0.7f, 1f); // Lighter blue
            public static readonly Color SelectionInactive = new Color(0.4f, 0.4f, 0.45f); // Subtle gray
            
            // Background Colors
            public static readonly Color SectionBackground = new Color(0.22f, 0.22f, 0.24f, 0.5f);
            public static readonly Color SeparatorColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            
            // Grid Cell Colors
            public static readonly Color EmptyCell = new Color(0.18f, 0.18f, 0.22f);
            public static readonly Color CellBorder = new Color(0.35f, 0.35f, 0.4f);
            public static readonly Color CellBorderHighlight = new Color(0.5f, 0.5f, 0.55f);
            
            // Blob Type Colors (index matches LevelEditorSchemas.BlobTypeSchema.All order: N,T,G,F,B,S,E,R)
            public static readonly Color[] BlobTypeColors = new Color[]
            {
                new Color(0.5f, 0.8f, 1f),      // Normal - light blue
                new Color(0.8f, 0.6f, 1f),      // Trail - purple
                new Color(0.9f, 0.9f, 0.9f),    // Ghost - white
                new Color(0.4f, 0.9f, 0.4f),    // Target - green
                new Color(0.6f, 0.35f, 0.35f),  // Bomb - dark red
                new Color(1f, 0.8f, 0.3f),      // Switch - gold
                new Color(0.9f, 0.4f, 0.3f),    // Enemy - orange-red
                new Color(0.5f, 0.5f, 0.5f),    // Rock - gray
            };
            
            // Blob Color Palette
            public static readonly Color[] BlobColorPalette = new Color[]
            {
                new Color(1f, 0.5f, 0.7f),      // Pink
                new Color(0.4f, 0.7f, 1f),      // Blue
                new Color(1f, 0.4f, 0.4f),      // Red
                new Color(0.4f, 0.9f, 0.9f),    // Cyan
                new Color(0.5f, 0.9f, 0.5f),    // Green
                new Color(1f, 0.9f, 0.4f),      // Yellow
                new Color(1f, 1f, 1f),          // White
                new Color(0.5f, 0.5f, 0.5f),    // Gray
            };
            
            // Tile Type Colors
            public static readonly Color[] TileTypeColors = new Color[]
            {
                new Color(0.25f, 0.25f, 0.3f),   // Normal
                new Color(0.2f, 0.2f, 0.25f),    // Train
                new Color(0.15f, 0.15f, 0.2f),   // Spike
                new Color(0.6f, 0.4f, 0.7f),    // Laser
                new Color(0.35f, 0.3f, 0.4f),   // Sigil
                new Color(0.5f, 0.35f, 0.25f),  // Sticky
                new Color(0.5f, 0.7f, 0.8f),    // Ice
                new Color(0.3f, 0.4f, 0.3f),     // Target
            };
            
            // GUIStyle Helpers
            public static GUIStyle GetHeaderStyle()
            {
                return new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 14,
                    alignment = TextAnchor.MiddleLeft
                };
            }
            
            public static GUIStyle GetSubtitleStyle()
            {
                return new GUIStyle(EditorStyles.miniBoldLabel)
                {
                    fontSize = 11
                };
            }
            
            public static GUIStyle GetHintStyle()
            {
                return new GUIStyle(EditorStyles.miniLabel)
                {
                    fontSize = 9,
                    normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
                };
            }
            
            public static GUIStyle GetToggleButtonStyle(bool isSelected)
            {
                var style = new GUIStyle(EditorStyles.miniButton);
                if (isSelected)
                {
                    style.normal.background = Texture2D.whiteTexture;
                    style.normal.textColor = Color.white;
                }
                return style;
            }
        }
        
        #endregion

        #region Private Fields
        
        private LevelData levelData;
        private BlobType selectedBlobType = BlobType.Normal;
        private BlobColor selectedBlobColor = BlobColor.Pink;
        private BlobColor selectedTrailColor = BlobColor.LightBlue;
        private BlobSize selectedBlobSize = BlobSize.Normal;
        private TileType selectedTileType = TileType.Normal;
        private bool isPlacingBlob = true;
        private Vector2 scrollPosition;

        private enum EditorMode { PaintBlob, PaintTile, Inspect }
        private EditorMode editorMode = EditorMode.PaintBlob;
        private Vector2Int? selectedCell;

        // Foldout states
        private bool showLevelInfo = true;
        private bool showScoring = false;
        private bool showTutorial = false;
        private bool showLevelEditor = true;
        private bool showRawData = false;
        
        #endregion

        #region Unity Lifecycle
        
        private void OnEnable()
        {
            levelData = (LevelData)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawHeader();
            EditorGUILayout.Space(SPACING_MEDIUM);

            DrawLevelInfoSection();
            EditorGUILayout.Space(SPACING_MEDIUM);

            DrawScoringSection();
            EditorGUILayout.Space(SPACING_MEDIUM);

            DrawTutorialSection();
            EditorGUILayout.Space(SPACING_LARGE);

            DrawGridEditor();
            EditorGUILayout.Space(SPACING_SMALL);

            DrawRawDataSection();

            serializedObject.ApplyModifiedProperties();
        }
        
        #endregion

        #region Helper Methods
        
        private SerializedProperty GetPropertySafe(string name)
        {
            return serializedObject.FindProperty(name);
        }
        
        private void DrawPropertyFieldSafe(SerializedProperty prop, string label = null, float width = 0)
        {
            if (prop == null)
            {
                EditorGUILayout.HelpBox($"Property '{label ?? "Unknown"}' not found.", MessageType.Warning);
                return;
            }
            
            if (width > 0)
                EditorGUILayout.PropertyField(prop, new GUIContent(label ?? prop.displayName), GUILayout.Width(width));
            else
                EditorGUILayout.PropertyField(prop, new GUIContent(label ?? prop.displayName));
        }

        private void DrawIntFieldSafe(SerializedProperty prop, string label = null, float width = 0)
        {
            if (prop == null)
            {
                EditorGUILayout.HelpBox($"Property '{label ?? "Unknown"}' not found.", MessageType.Warning);
                return;
            }

            if (prop.propertyType != SerializedPropertyType.Integer)
            {
                DrawPropertyFieldSafe(prop, label, width);
                return;
            }

            EditorGUI.BeginChangeCheck();
            int newValue = width > 0
                ? EditorGUILayout.IntField(new GUIContent(label ?? prop.displayName), prop.intValue, GUILayout.Width(width))
                : EditorGUILayout.IntField(new GUIContent(label ?? prop.displayName), prop.intValue);

            if (EditorGUI.EndChangeCheck())
            {
                prop.intValue = newValue;
            }
        }
        
        private void DrawSectionBox(Action content)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            content?.Invoke();
            EditorGUILayout.EndVertical();
        }
        
        private bool DrawToggleButton(string label, bool isSelected, float width = 0, float height = BUTTON_HEIGHT)
        {
            var originalColor = GUI.backgroundColor;
            GUI.backgroundColor = isSelected ? EditorStyleConfig.SelectionActive : Color.white;
            
            bool clicked = false;
            if (width > 0)
                clicked = GUILayout.Button(label, GUILayout.Width(width), GUILayout.Height(height));
            else
                clicked = GUILayout.Button(label, GUILayout.Height(height));
            
            GUI.backgroundColor = originalColor;
            return clicked;
        }
        
        private void DrawColorSwatch(Color color, bool isSelected, Action onClick, float size = 20f)
        {
            var originalColor = GUI.backgroundColor;
            GUI.backgroundColor = color;
            
            if (isSelected)
            {
                GUI.backgroundColor = Color.Lerp(color, Color.white, 0.3f);
            }
            
            if (GUILayout.Button("", GUILayout.Width(size), GUILayout.Height(size)))
            {
                onClick?.Invoke();
            }
            
            GUI.backgroundColor = originalColor;
        }
        
        #endregion

        #region Header & Sections
        
        private new void DrawHeader()
        {
            EditorGUILayout.BeginVertical();
            
            string levelName = string.IsNullOrEmpty(levelData.LevelName) ? "Unnamed Level" : levelData.LevelName;
            EditorGUILayout.LabelField($"Level {levelData.LevelNumber}: {levelName}", EditorStyleConfig.GetHeaderStyle());
            
            GUILayout.FlexibleSpace();
            
            int blobCount = levelData.Blobs != null ? levelData.Blobs.Count : 0;
            EditorGUILayout.LabelField($"{levelData.Width}x{levelData.Height} | {blobCount} blobs", 
                EditorStyles.miniLabel, GUILayout.Width(120));
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(SPACING_SMALL);
            Rect separatorRect = EditorGUILayout.GetControlRect(false, 2);
            EditorGUI.DrawRect(separatorRect, EditorStyleConfig.SeparatorColor);
        }

        private void DrawLevelInfoSection()
        {
            showLevelInfo = EditorGUILayout.BeginFoldoutHeaderGroup(showLevelInfo, "📋 Level Info & Grid Size");
            if (showLevelInfo)
            {
                EditorGUI.indentLevel++;
                DrawSectionBox(() =>
                {
                    // Level Number - full width
                    DrawPropertyFieldSafe(GetPropertySafe("LevelNumber"), "Level Number");
                    EditorGUILayout.Space(SPACING_SMALL);
                    
                    // Level Name - full width
                    DrawPropertyFieldSafe(GetPropertySafe("LevelName"), "Level Name");
                    EditorGUILayout.Space(SPACING_MEDIUM);
                    
                    // Grid Size Section
                    EditorGUILayout.LabelField("Grid Size", EditorStyleConfig.GetSubtitleStyle());
                    EditorGUILayout.Space(SPACING_SMALL);
                    
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.BeginVertical();
                    DrawIntFieldSafe(GetPropertySafe("Width"), "Width");
                    DrawIntFieldSafe(GetPropertySafe("Height"), "Height");
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(SPACING_SMALL);
                    
                    if (GUILayout.Button("3×3", GUILayout.Width(45))) SetGridSize(3, 3);
                    if (GUILayout.Button("5×5", GUILayout.Width(45))) SetGridSize(5, 5);
                    if (GUILayout.Button("7×7", GUILayout.Width(45))) SetGridSize(7, 7);
                    
                    EditorGUILayout.EndHorizontal();
                });
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawScoringSection()
        {
            showScoring = EditorGUILayout.BeginFoldoutHeaderGroup(showScoring, "⭐ Scoring Settings");
            if (showScoring)
            {
                EditorGUI.indentLevel++;
                DrawSectionBox(() =>
                {
                    var scoringProp = GetPropertySafe("Scoring");
                    var minMovesProp = GetPropertySafe("MinMoves");
                    
                    if (scoringProp != null)
                    {
                        // Min Moves on its own row
                        DrawPropertyFieldSafe(minMovesProp, "Minimum Moves");
                        EditorGUILayout.Space(SPACING_SMALL);
                        
                        // Scoring fields in two columns
                        EditorGUILayout.BeginVertical();
                        DrawPropertyFieldSafe(scoringProp.FindPropertyRelative("BaseScore"), "Base Score", FIELD_WIDTH_MEDIUM);

                        DrawPropertyFieldSafe(scoringProp.FindPropertyRelative("MovePenalty"), "Move Penalty", FIELD_WIDTH_MEDIUM);
                        EditorGUILayout.BeginVertical();
                        
                        EditorGUILayout.Space(SPACING_SMALL);
                        
                        DrawPropertyFieldSafe(scoringProp.FindPropertyRelative("StarThresholds"), "Star Thresholds");
                        EditorGUILayout.Space(SPACING_SMALL);
                        
                        EditorGUILayout.BeginVertical();
                        DrawPropertyFieldSafe(scoringProp.FindPropertyRelative("GemBonus"), "Gem Bonus", FIELD_WIDTH_MEDIUM);
                        DrawPropertyFieldSafe(scoringProp.FindPropertyRelative("UndoPenalty"), "Undo Penalty", FIELD_WIDTH_MEDIUM);
                        EditorGUILayout.EndVertical();
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Scoring object not found. Please ensure LevelData has a Scoring field.", MessageType.Warning);
                    }
                });
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawTutorialSection()
        {
            showTutorial = EditorGUILayout.BeginFoldoutHeaderGroup(showTutorial, "📖 Tutorial Settings");
            if (showTutorial)
            {
                EditorGUI.indentLevel++;
                DrawSectionBox(() =>
                {
                    DrawPropertyFieldSafe(GetPropertySafe("IsTutorial"), "Is Tutorial");
                    
                    if (levelData.IsTutorial)
                    {
                        EditorGUILayout.Space(SPACING_SMALL);
                        DrawPropertyFieldSafe(GetPropertySafe("TutorialSteps"), "Tutorial Steps", 0);
                        
                        if (EditorGUI.EndChangeCheck())
                        {
                            EditorUtility.SetDirty(levelData);
                            AssetDatabase.SaveAssets();
                        }
                    }
                });
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawRawDataSection()
        {
            showRawData = EditorGUILayout.BeginFoldoutHeaderGroup(showRawData, "📦 Raw Data (Advanced)");
            if (showRawData)
            {
                EditorGUI.indentLevel++;
                DrawSectionBox(() =>
                {
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Export JSON", GUILayout.Width(100), GUILayout.Height(BUTTON_HEIGHT)))
                    {
                        string path = EditorUtility.SaveFilePanel("Export Level JSON", "Assets/Resources/Levels", "level_" + levelData.LevelNumber + ".json", "json");
                        if (!string.IsNullOrEmpty(path))
                        {
                            System.IO.File.WriteAllText(path, LevelDataJsonSerializer.ToJson(levelData));
                            AssetDatabase.Refresh();
                        }
                    }
                    if (GUILayout.Button("Import JSON", GUILayout.Width(100), GUILayout.Height(BUTTON_HEIGHT)))
                    {
                        string path = EditorUtility.OpenFilePanel("Import Level JSON", "Assets/Levels", "json");
                        if (!string.IsNullOrEmpty(path))
                        {
                            string json = System.IO.File.ReadAllText(path);
                            Undo.RecordObject(levelData, "Import JSON");
                            LevelDataJsonSerializer.FromJson(levelData, json);
                            
                            EditorUtility.SetDirty(levelData);
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.Space(SPACING_SMALL);
                    DrawPropertyFieldSafe(GetPropertySafe("Blobs"), "Blobs");
                    EditorGUILayout.Space(SPACING_SMALL);
                    DrawPropertyFieldSafe(GetPropertySafe("Tiles"), "Tiles");
                    EditorGUILayout.Space(SPACING_SMALL);
                    DrawPropertyFieldSafe(GetPropertySafe("LaserLinks"), "Laser Links");
                });
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
        
        #endregion

        #region Grid Editor
        
        private void DrawGridEditor()
        {
            showLevelEditor = EditorGUILayout.BeginFoldoutHeaderGroup(showLevelEditor, "🎮 Level Editor");
            if (showLevelEditor)
            {
                EditorGUI.indentLevel++;
                DrawSectionBox(() =>
                {
                    DrawInstructions();
                    EditorGUILayout.Space(SPACING_MEDIUM);
                    
                    DrawToolSelector();
                    EditorGUILayout.Space(SPACING_MEDIUM);
                    
                    DrawPalette();
                    EditorGUILayout.Space(SPACING_MEDIUM);
                    
                    DrawGrid();

                    if (editorMode == EditorMode.Inspect && selectedCell.HasValue)
                    {
                        EditorGUILayout.Space(SPACING_MEDIUM);
                        DrawInspectPanel();
                    }
                });
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawInspectPanel()
        {
            if (!selectedCell.HasValue) return;
            Vector2Int pos = selectedCell.Value;
            BlobSpawnData blob = GetBlobAt(pos);
            TileSpawnData tile = GetTileAt(pos);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Cell " + pos.x + ", " + pos.y, EditorStyleConfig.GetSubtitleStyle());

            if (blob != null)
            {
                EditorGUILayout.Space(SPACING_SMALL);
                EditorGUILayout.LabelField("Blob", EditorStyles.boldLabel);
                EditorGUI.BeginChangeCheck();
                Undo.RecordObject(levelData, "Edit Blob");
                var schema = BlobTypeSchema.Get(blob.Type);
                blob.Type = (BlobType)EditorGUILayout.EnumPopup("Type", blob.Type);
                var newSchema = BlobTypeSchema.Get(blob.Type);
                if (newSchema.UsesColor)
                    blob.Color = (BlobColor)EditorGUILayout.EnumPopup("Color", blob.Color);
                else
                    blob.Color = newSchema.DefaultColor;
                if (newSchema.UsesSize)
                    blob.Size = (BlobSize)EditorGUILayout.EnumPopup("Size", blob.Size);
                else
                    blob.Size = newSchema.DefaultSize;
                if (newSchema.UsesTrailColor)
                {
                    var trailColor = blob.GetProperty<BlobColor>(LevelDataKeys.Properties.TrailColor);
                    var newTrail = (BlobColor)EditorGUILayout.EnumPopup("Trail color", trailColor);
                    blob.SetProperty(LevelDataKeys.Properties.TrailColor, newTrail);
                }
                if (EditorGUI.EndChangeCheck())
                    EditorUtility.SetDirty(levelData);
                if (GUILayout.Button("Remove Blob", GUILayout.Width(100)))
                {
                    levelData.Blobs.RemoveAll(b => b.GridPosition == pos);
                    selectedCell = null;
                    EditorUtility.SetDirty(levelData);
                }
            }
            else
            {
                if (GUILayout.Button("Add Blob", GUILayout.Width(90)))
                {
                    Undo.RecordObject(levelData, "Add Blob");
                    if (levelData.Blobs == null) levelData.Blobs = new System.Collections.Generic.List<BlobSpawnData>();
                    var schema = BlobTypeSchema.Get(selectedBlobType);
                    var newBlob = new BlobSpawnData
                    {
                        GridPosition = pos,
                        Type = selectedBlobType,
                        Color = schema.UsesColor ? selectedBlobColor : schema.DefaultColor,
                        Size = schema.UsesSize ? selectedBlobSize : schema.DefaultSize
                    };
                    if (schema.UsesTrailColor)
                        newBlob.SetProperty(LevelDataKeys.Properties.TrailColor, selectedTrailColor);
                    levelData.Blobs.Add(newBlob);
                    levelData.Tiles ??= new System.Collections.Generic.List<TileSpawnData>();
                    if (GetTileAt(pos) == null)
                        levelData.Tiles.Add(new TileSpawnData { GridPosition = pos, Type = TileType.Normal });
                    EditorUtility.SetDirty(levelData);
                }
            }

            EditorGUILayout.Space(SPACING_SMALL);
            if (tile != null)
            {
                EditorGUILayout.LabelField("Tile", EditorStyles.boldLabel);
                EditorGUI.BeginChangeCheck();
                Undo.RecordObject(levelData, "Edit Tile");
                tile.Type = (TileType)EditorGUILayout.EnumPopup("Type", tile.Type);
                var tSchema = TileTypeSchema.Get(tile.Type);
                if (tSchema.UsesLaserId)
                {
                    var id = tile.GetProperty<string>(LevelDataKeys.Properties.LaserId);
                    var newId = EditorGUILayout.TextField("Laser ID", id ?? "");
                    tile.SetProperty(LevelDataKeys.Properties.LaserId, newId);
                }
                if (tSchema.UsesLaserColor)
                {
                    var lc = tile.GetProperty<BlobColor>(LevelDataKeys.Properties.Color);
                    var newLc = (BlobColor)EditorGUILayout.EnumPopup("Laser color", lc);
                    tile.SetProperty(LevelDataKeys.Properties.Color, newLc);
                }
                if (EditorGUI.EndChangeCheck())
                    EditorUtility.SetDirty(levelData);
                if (GUILayout.Button("Remove Tile", GUILayout.Width(100)))
                {
                    levelData.Tiles.RemoveAll(t => t.GridPosition == pos);
                    EditorUtility.SetDirty(levelData);
                }
            }
            else
            {
                if (GUILayout.Button("Add Tile", GUILayout.Width(90)))
                {
                    Undo.RecordObject(levelData, "Add Tile");
                    levelData.Tiles ??= new System.Collections.Generic.List<TileSpawnData>();
                    var tileData = new TileSpawnData { GridPosition = pos, Type = selectedTileType };
                    var tSchema = TileTypeSchema.Get(selectedTileType);
                    if (tSchema.UsesLaserId)
                        tileData.SetProperty(LevelDataKeys.Properties.LaserId, "laser_" + pos.x + "_" + pos.y);
                    if (tSchema.UsesLaserColor)
                        tileData.SetProperty(LevelDataKeys.Properties.Color, BlobColor.Pink);
                    levelData.Tiles.Add(tileData);
                    EditorUtility.SetDirty(levelData);
                }
            }

            EditorGUILayout.EndVertical();
        }
        
        private void DrawInstructions()
        {
            EditorGUILayout.LabelField("💡 Blob/Tile: Left-click place, Right-click remove. Inspect: click cell to select and edit.", 
                EditorStyleConfig.GetHintStyle());
        }
        
        private void DrawToolSelector()
        {
            EditorGUILayout.BeginHorizontal();
            
            EditorGUILayout.LabelField("Tool:", GUILayout.Width(50));
            
            if (DrawToggleButton("Blob", editorMode == EditorMode.PaintBlob, 60, BUTTON_HEIGHT))
            {
                editorMode = EditorMode.PaintBlob;
                isPlacingBlob = true;
            }
            if (DrawToggleButton("Tile", editorMode == EditorMode.PaintTile, 60, BUTTON_HEIGHT))
            {
                editorMode = EditorMode.PaintTile;
                isPlacingBlob = false;
            }
            if (DrawToggleButton("Inspect", editorMode == EditorMode.Inspect, 60, BUTTON_HEIGHT))
                editorMode = EditorMode.Inspect;
            
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("Clear Blobs", GUILayout.Width(90), GUILayout.Height(BUTTON_HEIGHT)))
            {
                if (EditorUtility.DisplayDialog("Clear Blobs", "Are you sure you want to clear all blobs?", "Yes", "Cancel"))
                {
                    Undo.RecordObject(levelData, "Clear Blobs");
                    if (levelData.Blobs != null) levelData.Blobs.Clear();
                    EditorUtility.SetDirty(levelData);
                }
            }
            
            if (GUILayout.Button("Clear Tiles", GUILayout.Width(90), GUILayout.Height(BUTTON_HEIGHT)))
            {
                if (EditorUtility.DisplayDialog("Clear Tiles", "Are you sure you want to clear all tiles?", "Yes", "Cancel"))
                {
                    Undo.RecordObject(levelData, "Clear Tiles");
                    if (levelData.Tiles != null) levelData.Tiles.Clear();
                    EditorUtility.SetDirty(levelData);
                }
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawPalette()
        {
            if (editorMode == EditorMode.Inspect)
                return;
            if (isPlacingBlob)
                DrawBlobPalette();
            else
                DrawTilePalette();
        }

        private void DrawBlobPalette()
        {
            var schema = BlobTypeSchema.Get(selectedBlobType);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Type Selection (from schema)
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Type:", GUILayout.Width(50));
            var allBlob = BlobTypeSchema.All;
            for (int i = 0; i < allBlob.Count; i++)
            {
                var s = allBlob[i];
                bool isSelected = selectedBlobType == s.Type;
                var originalColor = GUI.backgroundColor;
                if (i < EditorStyleConfig.BlobTypeColors.Length)
                    GUI.backgroundColor = EditorStyleConfig.BlobTypeColors[i];
                if (isSelected)
                    GUI.backgroundColor = Color.Lerp(GUI.backgroundColor, Color.white, 0.3f);
                var btnStyle = new GUIStyle(GUI.skin.button);
                if (isSelected) btnStyle.fontStyle = FontStyle.Bold;
                if (GUILayout.Button(s.ShortLabel, btnStyle, GUILayout.Width(28), GUILayout.Height(BUTTON_HEIGHT)))
                {
                    selectedBlobType = s.Type;
                    selectedBlobColor = s.DefaultColor;
                    selectedBlobSize = s.DefaultSize;
                    selectedTrailColor = BlobColor.LightBlue;
                }
                GUI.backgroundColor = originalColor;
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(SPACING_SMALL);
            
            if (schema.UsesColor)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Color:", GUILayout.Width(50));
                var colorOrder = new[] { BlobColor.Pink, BlobColor.Blue, BlobColor.Red, BlobColor.LightBlue, BlobColor.Green, BlobColor.Yellow, BlobColor.Purple, BlobColor.Orange };
                for (int i = 0; i < colorOrder.Length && i < EditorStyleConfig.BlobColorPalette.Length; i++)
                {
                    var c = colorOrder[i];
                    bool isSelected = selectedBlobColor == c;
                    DrawColorSwatch(EditorStyleConfig.BlobColorPalette[i], isSelected, () => selectedBlobColor = c, 24f);
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.LabelField("Color: (none)", EditorStyles.miniLabel);
            }

            if (schema.UsesSize)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Size:", GUILayout.Width(50));
                selectedBlobSize = (BlobSize)EditorGUILayout.EnumPopup(selectedBlobSize, GUILayout.Width(80));
                EditorGUILayout.EndHorizontal();
            }

            if (schema.UsesTrailColor)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Trail color:", GUILayout.Width(70));
                var trailColorOrder = new[] { BlobColor.Pink, BlobColor.Blue, BlobColor.Red, BlobColor.LightBlue, BlobColor.Green, BlobColor.Yellow, BlobColor.Purple, BlobColor.Orange };
                for (int i = 0; i < trailColorOrder.Length && i < EditorStyleConfig.BlobColorPalette.Length; i++)
                {
                    var c = trailColorOrder[i];
                    bool isSelected = selectedTrailColor == c;
                    DrawColorSwatch(EditorStyleConfig.BlobColorPalette[i], isSelected, () => selectedTrailColor = c, 22f);
                }
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.LabelField($"[{selectedBlobType}" + (schema.UsesColor ? $" - {selectedBlobColor}" : "") + "]", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }

        private void DrawTilePalette()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Type:", GUILayout.Width(50));
            
            string[] tileNames = System.Enum.GetNames(typeof(TileType));
            for (int i = 0; i < tileNames.Length; i++)
            {
                bool isSelected = selectedTileType == (TileType)i;
                var originalColor = GUI.backgroundColor;
                
                if (isSelected)
                {
                    GUI.backgroundColor = EditorStyleConfig.SelectionActive;
                }
                else if (i < EditorStyleConfig.TileTypeColors.Length)
                {
                    GUI.backgroundColor = EditorStyleConfig.TileTypeColors[i];
                }
                else
                {
                    GUI.backgroundColor = Color.gray;
                }
                
                if (GUILayout.Button(tileNames[i], GUILayout.Height(BUTTON_HEIGHT)))
                {
                    selectedTileType = (TileType)i;
                }
                
                GUI.backgroundColor = originalColor;
            }
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField($"[{selectedTileType}]", EditorStyles.miniLabel, GUILayout.Width(120));
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }

        private void DrawGrid()
        {
            if (levelData.Width <= 0 || levelData.Height <= 0)
            {
                EditorGUILayout.HelpBox("Grid size must be greater than 0.", MessageType.Warning);
                return;
            }
            
            float gridWidth = levelData.Width * (CELL_SIZE + CELL_SPACING);
            float gridHeight = levelData.Height * (CELL_SIZE + CELL_SPACING);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, 
                GUILayout.Height(Mathf.Min(gridHeight + 20, GRID_MAX_HEIGHT)));

            Rect gridRect = GUILayoutUtility.GetRect(gridWidth, gridHeight);

            // Draw grid cells (Y is flipped - 0 is bottom)
            for (int y = levelData.Height - 1; y >= 0; y--)
            {
                for (int x = 0; x < levelData.Width; x++)
                {
                    float cellX = gridRect.x + x * (CELL_SIZE + CELL_SPACING);
                    float cellY = gridRect.y + (levelData.Height - 1 - y) * (CELL_SIZE + CELL_SPACING);
                    Rect cellRect = new Rect(cellX, cellY, CELL_SIZE, CELL_SIZE);

                    Vector2Int pos = new Vector2Int(x, y);
                    TileSpawnData tileData = GetTileAt(pos);
                    BlobSpawnData blobData = GetBlobAt(pos);

                    // Draw tile background
                    Color tileColor = GetTileColor(tileData);
                    EditorGUI.DrawRect(cellRect, tileColor);

                    // Draw blob if exists
                    if (blobData != null)
                    {
                        DrawBlobInCell(cellRect, blobData);
                    }

                    if (selectedCell.HasValue && selectedCell.Value == pos)
                    {
                        var highlightRect = new Rect(cellRect.x - 1, cellRect.y - 1, cellRect.width + 2, cellRect.height + 2);
                        Handles.color = EditorStyleConfig.SelectionActive;
                        Handles.DrawSolidRectangleWithOutline(highlightRect, new Color(0.2f, 0.6f, 0.9f, 0.2f), EditorStyleConfig.SelectionActive);
                    }

                    DrawCellBorder(cellRect);

                    // Handle clicks
                    if (Event.current.type == EventType.MouseDown && cellRect.Contains(Event.current.mousePosition))
                    {
                        HandleCellClick(pos, Event.current.button);
                        Event.current.Use();
                        Repaint();
                    }

                    // Draw coordinates
                    GUIStyle coordStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        fontSize = 8,
                        normal = { textColor = new Color(1, 1, 1, 0.5f) }
                    };
                    GUI.Label(new Rect(cellX + 2, cellY + CELL_SIZE - 11, 25, 10), $"{x},{y}", coordStyle);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private static Color GetDisplayColorForBlobColor(BlobColor c)
        {
            if (c == BlobColor.Blank || c == BlobColor.None)
                return new Color(0.5f, 0.5f, 0.5f);
            var order = new[] { BlobColor.Pink, BlobColor.Blue, BlobColor.Red, BlobColor.LightBlue, BlobColor.Green, BlobColor.Yellow, BlobColor.Purple, BlobColor.Orange };
            for (int i = 0; i < order.Length && i < EditorStyleConfig.BlobColorPalette.Length; i++)
                if (order[i] == c) return EditorStyleConfig.BlobColorPalette[i];
            return EditorStyleConfig.BlobColorPalette[0];
        }

        private void DrawBlobInCell(Rect cellRect, BlobSpawnData blob)
        {
            float padding = 6f;
            Rect blobRect = new Rect(
                cellRect.x + padding,
                cellRect.y + padding,
                cellRect.width - padding * 2,
                cellRect.height - padding * 2
            );

            Color blobColor = GetDisplayColorForBlobColor(blob.Color);
            if (blob.Type == BlobType.Ghost)
                blobColor.a = 0.6f;
            else if (blob.Type == BlobType.Rock || blob.Type == BlobType.Bomb)
                blobColor = new Color(0.4f, 0.35f, 0.3f);

            EditorGUI.DrawRect(blobRect, blobColor);

            string typeLabel = BlobTypeSchema.Get(blob.Type).ShortLabel;
            GUIStyle centerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16,
                normal = { textColor = Color.white }
            };
            Rect shadowRect = new Rect(blobRect.x + 1, blobRect.y + 1, blobRect.width, blobRect.height);
            GUIStyle shadowStyle = new GUIStyle(centerStyle) { normal = { textColor = Color.black } };
            GUI.Label(shadowRect, typeLabel, shadowStyle);
            GUI.Label(blobRect, typeLabel, centerStyle);

            if (blob.Type == BlobType.Trail)
            {
                var trailColor = blob.GetProperty<BlobColor>(LevelDataKeys.Properties.TrailColor);
                var tc = GetDisplayColorForBlobColor(trailColor);
                Rect trailMarker = new Rect(blobRect.xMax - 8, blobRect.y, 6, 6);
                EditorGUI.DrawRect(trailMarker, tc);
            }
        }

        private void DrawCellBorder(Rect cellRect)
        {
            Handles.color = EditorStyleConfig.CellBorder;
            Handles.DrawLine(new Vector3(cellRect.x, cellRect.y), new Vector3(cellRect.xMax, cellRect.y));
            Handles.DrawLine(new Vector3(cellRect.xMax, cellRect.y), new Vector3(cellRect.xMax, cellRect.yMax));
            Handles.DrawLine(new Vector3(cellRect.xMax, cellRect.yMax), new Vector3(cellRect.x, cellRect.yMax));
            Handles.DrawLine(new Vector3(cellRect.x, cellRect.yMax), new Vector3(cellRect.x, cellRect.y));
        }

        private Color GetTileColor(TileSpawnData tile)
        {
            if (tile == null)
                return EditorStyleConfig.EmptyCell;

            int tileIndex = (int)tile.Type;
            if (tileIndex >= 0 && tileIndex < EditorStyleConfig.TileTypeColors.Length)
                return EditorStyleConfig.TileTypeColors[tileIndex];
            
            return EditorStyleConfig.TileTypeColors[0]; // Default to Normal
        }
        
        #endregion

        #region Grid Interaction
        
        private void HandleCellClick(Vector2Int pos, int mouseButton)
        {
            if (editorMode == EditorMode.Inspect)
            {
                if (mouseButton == 0)
                    selectedCell = pos;
                return;
            }

            Undo.RecordObject(levelData, "Edit Level");

            if (mouseButton == 0) // Left click - place
            {
                if (isPlacingBlob)
                {
                    var schema = BlobTypeSchema.Get(selectedBlobType);
                    if (levelData.Blobs == null) levelData.Blobs = new System.Collections.Generic.List<BlobSpawnData>();
                    levelData.Blobs.RemoveAll(b => b.GridPosition == pos);
                    var blob = new BlobSpawnData
                    {
                        GridPosition = pos,
                        Type = selectedBlobType,
                        Color = schema.UsesColor ? selectedBlobColor : schema.DefaultColor,
                        Size = schema.UsesSize ? selectedBlobSize : schema.DefaultSize
                    };
                    if (schema.UsesTrailColor)
                        blob.SetProperty(LevelDataKeys.Properties.TrailColor, selectedTrailColor);
                    levelData.Blobs.Add(blob);
                    levelData.Tiles ??= new System.Collections.Generic.List<TileSpawnData>();
                    if (GetTileAt(pos) == null)
                        levelData.Tiles.Add(new TileSpawnData { GridPosition = pos, Type = TileType.Normal });
                }
                else
                {
                    levelData.Tiles ??= new System.Collections.Generic.List<TileSpawnData>();
                    levelData.Tiles.RemoveAll(t => t.GridPosition == pos);
                    var tile = new TileSpawnData { GridPosition = pos, Type = selectedTileType };
                    var tileSchema = TileTypeSchema.Get(selectedTileType);
                    if (tileSchema.UsesLaserId)
                        tile.SetProperty(LevelDataKeys.Properties.LaserId, "laser_" + pos.x + "_" + pos.y);
                    if (tileSchema.UsesLaserColor)
                        tile.SetProperty(LevelDataKeys.Properties.Color, BlobColor.Pink);
                    levelData.Tiles.Add(tile);
                }
            }
            else if (mouseButton == 1) // Right click - remove
            {
                if (isPlacingBlob && levelData.Blobs != null)
                    levelData.Blobs.RemoveAll(b => b.GridPosition == pos);
                else if (!isPlacingBlob && levelData.Tiles != null)
                    levelData.Tiles.RemoveAll(t => t.GridPosition == pos);
            }

            EditorUtility.SetDirty(levelData);
        }

        private void SetGridSize(int w, int h)
        {
            Undo.RecordObject(levelData, "Set Grid Size");
            levelData.Width = w;
            levelData.Height = h;
            EditorUtility.SetDirty(levelData);
        }

        private BlobSpawnData GetBlobAt(Vector2Int pos)
        {
            if (levelData.Blobs == null) return null;
            return levelData.Blobs.Find(b => b.GridPosition == pos);
        }

        private TileSpawnData GetTileAt(Vector2Int pos)
        {
            if (levelData.Tiles == null) return null;
            return levelData.Tiles.Find(t => t.GridPosition == pos);
        }
        
        #endregion
    }
}
