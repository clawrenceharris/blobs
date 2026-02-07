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
            
            // Blob Type Colors
            public static readonly Color[] BlobTypeColors = new Color[]
            {
                new Color(0.5f, 0.8f, 1f),      // Normal - light blue
                new Color(0.8f, 0.6f, 1f),      // Trail - purple
                new Color(0.9f, 0.9f, 0.9f),    // Ghost - white
                new Color(0.4f, 0.9f, 0.4f),    // Flag - green
                new Color(0.5f, 0.5f, 0.5f),    // Rock - gray
                new Color(1f, 0.8f, 0.3f),      // Switch - gold
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
        private TileType selectedTileType = TileType.Normal;
        private bool isPlacingBlob = true;
        private Vector2 scrollPosition;

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
                });
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
        
        private void DrawInstructions()
        {
            EditorGUILayout.LabelField("💡 Left-click: Place | Right-click: Remove | Keyboard: N=Normal T=Trail G=Ghost F=Flag R=Rock S=Switch", 
                EditorStyleConfig.GetHintStyle());
        }
        
        private void DrawToolSelector()
        {
            EditorGUILayout.BeginHorizontal();
            
            EditorGUILayout.LabelField("Tool:", GUILayout.Width(50));
            
            if (DrawToggleButton("🔵 Blob", isPlacingBlob, 80, BUTTON_HEIGHT))
                isPlacingBlob = true;
            
            if (DrawToggleButton("⬛ Tile", !isPlacingBlob, 80, BUTTON_HEIGHT))
                isPlacingBlob = false;
            
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
            if (isPlacingBlob)
            {
                DrawBlobPalette();
            }
            else
            {
                DrawTilePalette();
            }
        }
        
        private void DrawBlobPalette()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Type Selection
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Type:", GUILayout.Width(50));
            
            BlobType[] blobTypes = { BlobType.Normal, BlobType.Trail, BlobType.Ghost, BlobType.Flag, BlobType.Bomb, BlobType.Switch };
            for (int i = 0; i < blobTypes.Length; i++)
            {
                bool isSelected = selectedBlobType == blobTypes[i];
                var originalColor = GUI.backgroundColor;
                GUI.backgroundColor = EditorStyleConfig.BlobTypeColors[i];
                
                if (isSelected)
                {
                    GUI.backgroundColor = Color.Lerp(EditorStyleConfig.BlobTypeColors[i], Color.white, 0.3f);
                }
                
                var btnStyle = new GUIStyle(GUI.skin.button);
                if (isSelected)
                    btnStyle.fontStyle = FontStyle.Bold;
                
                if (GUILayout.Button(blobTypes[i].ToString()[..1], btnStyle, GUILayout.Width(28), GUILayout.Height(BUTTON_HEIGHT)))
                {
                    selectedBlobType = blobTypes[i];
                }
                
                GUI.backgroundColor = originalColor;
            }
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(SPACING_SMALL);
            
            // Color Selection
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Color:", GUILayout.Width(50));
            
            string[] colorNames = { "Pk", "Bl", "Rd", "Cy", "Gr", "Yl", "Wh", "Gy" };
            for (int i = 0; i < colorNames.Length; i++)
            {
                bool isSelected = selectedBlobColor == (BlobColor)i;
                DrawColorSwatch(EditorStyleConfig.BlobColorPalette[i], isSelected, 
                    () => selectedBlobColor = (BlobColor)i, 24f);
            }
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField($"[{selectedBlobType} - {selectedBlobColor}]", EditorStyles.miniLabel, GUILayout.Width(120));
            EditorGUILayout.EndHorizontal();
            
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

                    // Draw border with better contrast
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

        private void DrawBlobInCell(Rect cellRect, BlobSpawnData blob)
        {
            float padding = 6f;
            Rect blobRect = new Rect(
                cellRect.x + padding,
                cellRect.y + padding,
                cellRect.width - padding * 2,
                cellRect.height - padding * 2
            );

            Color blobColor = EditorStyleConfig.BlobColorPalette[(int)blob.Color];
            
            // Modify for special types
            if (blob.Type == BlobType.Ghost)
            {
                blobColor.a = 0.6f;
            }
            else if (blob.Type == BlobType.Rock)
            {
                blobColor = new Color(0.4f, 0.35f, 0.3f);
            }

            // Draw blob circle
            EditorGUI.DrawRect(blobRect, blobColor);

            // Draw type indicator with shadow
            string typeLabel = blob.Type.ToString()[..1];
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
            Undo.RecordObject(levelData, "Edit Level");

            if (mouseButton == 0) // Left click - place
            {
                if (isPlacingBlob)
                {
                    if (levelData.Blobs == null) levelData.Blobs = new System.Collections.Generic.List<BlobSpawnData>();
                    levelData.Blobs.RemoveAll(b => b.GridPosition == pos);
                    levelData.Blobs.Add(new BlobSpawnData
                    {
                        GridPosition = pos,
                        Type = selectedBlobType,
                        Color = selectedBlobColor,
                        Size = BlobSize.Normal
                    });
                    // Ensure a tile exists at blob position (default Normal unless already set)
                    levelData.Tiles ??= new System.Collections.Generic.List<TileSpawnData>();
                    if (GetTileAt(pos) == null)
                        levelData.Tiles.Add(new TileSpawnData { GridPosition = pos, Type = TileType.Normal });
                }
                else
                {
                    levelData.Tiles ??= new System.Collections.Generic.List<TileSpawnData>();
                    levelData.Tiles.RemoveAll(t => t.GridPosition == pos);
                    levelData.Tiles.Add(new TileSpawnData
                    {
                        GridPosition = pos,
                        Type = selectedTileType
                    });
                }
            }
            else if (mouseButton == 1) // Right click - remove
            {
                if (isPlacingBlob && levelData.Blobs != null)
                {
                    levelData.Blobs.RemoveAll(b => b.GridPosition == pos);
                }
                else if (!isPlacingBlob && levelData.Tiles != null)
                {
                    levelData.Tiles.RemoveAll(t => t.GridPosition == pos);
                }
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
