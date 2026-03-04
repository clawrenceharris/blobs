#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Blobs.Editor
{
    /// <summary>
    /// Imports level data from JSON files (Assets/Levels/level_*.json)
    /// and creates/updates LevelData ScriptableObjects in Assets/Resources/Levels/.
    /// Run via: Tools > Import JSON Levels
    /// </summary>
    public static class JsonLevelImporter
    {
        private const string JSON_FOLDER   = "Assets/Levels";
        private const string OUTPUT_FOLDER = "Assets/Resources/Levels";

        [MenuItem("Tools/Import JSON Levels")]
        public static void ImportAll()
        {
            // Ensure output folder exists
            if (!AssetDatabase.IsValidFolder(OUTPUT_FOLDER))
            {
                string[] parts = OUTPUT_FOLDER.Split('/');
                string soFar = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    string next = soFar + "/" + parts[i];
                    if (!AssetDatabase.IsValidFolder(next))
                        AssetDatabase.CreateFolder(soFar, parts[i]);
                    soFar = next;
                }
            }

            string[] jsonFiles = Directory.GetFiles(
                Path.Combine(Application.dataPath, "../" + JSON_FOLDER),
                "level_*.json",
                SearchOption.TopDirectoryOnly);

            int importedCount = 0;
            foreach (string path in jsonFiles)
            {
                try
                {
                    string json = File.ReadAllText(path);
                    LevelJson raw = JsonUtility.FromJson<LevelJson>(json);
                    if (raw == null)
                    {
                        Debug.LogWarning($"[JsonLevelImporter] Could not parse: {path}");
                        continue;
                    }

                    // Use filename number as the authoritative level number.
                    // Catches copy-paste bugs (e.g. level_9.json has levelNum=8).
                    var match = System.Text.RegularExpressions.Regex.Match(
                        Path.GetFileNameWithoutExtension(path), @"\d+");
                    if (match.Success)
                    {
                        int filenameNum = int.Parse(match.Value);
                        if (filenameNum != raw.levelNum)
                        {
                            Debug.LogWarning($"[JsonLevelImporter] {Path.GetFileName(path)}" +
                                $" has levelNum={raw.levelNum} but filename says {filenameNum}. Using {filenameNum}.");
                            raw.levelNum = filenameNum;
                        }
                    }

                    string assetPath = $"{OUTPUT_FOLDER}/Level_{raw.levelNum}.asset";
                    LevelData data   = AssetDatabase.LoadAssetAtPath<LevelData>(assetPath);
                    bool isNew       = data == null;
                    if (isNew)
                        data = ScriptableObject.CreateInstance<LevelData>();

                    ApplyJsonToLevelData(raw, data);

                    if (isNew)
                        AssetDatabase.CreateAsset(data, assetPath);
                    else
                        EditorUtility.SetDirty(data);

                    importedCount++;
                    Debug.Log($"[JsonLevelImporter] {(isNew ? "Created" : "Updated")}: {assetPath}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"[JsonLevelImporter] Error processing {path}: {e.Message}\n{e.StackTrace}");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog(
                "Import Complete",
                $"Successfully imported {importedCount} level(s) into {OUTPUT_FOLDER}.",
                "OK");
        }

        // ─────────────────────────────────────────────────────────────────────────

        private static void ApplyJsonToLevelData(LevelJson raw, LevelData data)
        {
            data.LevelNumber = raw.levelNum;
            data.Width       = raw.width;
            data.Height      = raw.height;
            data.MinMoves    = raw.minMoves;
            data.IsTutorial  = raw.isTutorial;

            data.Scoring = new Scoring
            {
                BaseScore      = raw.scoring?.baseScore   ?? 0,
                MovePenalty    = raw.scoring?.movePenalty ?? 0,
                GemBonus       = raw.scoring?.gemBonus    ?? 0,
                StarThresholds = raw.scoring?.starThresholds ?? Array.Empty<int>(),
            };

            // Tutorial Steps
            data.TutorialSteps = Array.Empty<TutorialStep>();
            if (raw.tutorialSteps != null && raw.tutorialSteps.Length > 0)
            {
                var steps = new TutorialStep[raw.tutorialSteps.Length];
                for (int i = 0; i < raw.tutorialSteps.Length; i++)
                {
                    var s = raw.tutorialSteps[i];
                    steps[i] = new TutorialStep
                    {
                        TopText    = s.topText    ?? "",
                        BottomText = s.bottomText ?? "",
                        StartX     = s.startX,
                        StartY     = s.startY,
                        EndX       = s.endX,
                        EndY       = s.endY,
                    };
                }
                data.TutorialSteps = steps;
            }

            // Blobs — delegate key mapping to LevelDataKeys (single source of truth)
            data.Blobs = new List<BlobSpawnData>();
            if (raw.blobs != null)
            {
                foreach (var b in raw.blobs)
                {
                    BlobType blobType = LevelDataKeys.Types.blobTypeMap.TryGetValue(b.t ?? "", out var bt)
                        ? bt : BlobType.Normal;
                    BlobColor blobColor = LevelDataKeys.BlobColors.blobColorMap.TryGetValue(b.c ?? "", out var bc)
                        ? bc : BlobColor.Pink;
                    BlobSize blobSize = LevelDataKeys.BlobSizes.blobSizeMap.TryGetValue(b.s ?? "", out var bs)
                        ? bs : BlobSize.Normal;

                    data.Blobs.Add(new BlobSpawnData
                    {
                        GridPosition = new Vector2Int(b.x, b.y),
                        Type         = blobType,
                        Color        = blobColor,
                        Size         = blobSize,
                    });
                }
            }

            // Tiles — delegate key mapping to LevelDataKeys
            data.Tiles = new List<TileSpawnData>();
            if (raw.tiles != null)
            {
                foreach (var t in raw.tiles)
                {
                    TileType tileType = LevelDataKeys.Types.tileTypeMap.TryGetValue(t.t ?? "", out var tt)
                        ? tt : TileType.Normal;

                    data.Tiles.Add(new TileSpawnData
                    {
                        GridPosition = new Vector2Int(t.x, t.y),
                        Type         = tileType,
                    });
                }
            }

            // Laser links — none in current JSON data, preserve list
            data.LaserLinks = new List<LaserLink>();
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Internal JSON POCOs (only used during import, kept private)
        // ─────────────────────────────────────────────────────────────────────────

        [Serializable]
        private class LevelJson
        {
            public int    levelNum;
            public int    width;
            public int    height;
            public int    minMoves;
            public bool   isTutorial;
            public ScoringJson        scoring;
            public TutorialStepJson[] tutorialSteps;
            public BlobJson[]         blobs;
            public TileJson[]         tiles;
        }

        [Serializable]
        private class ScoringJson
        {
            public int   baseScore;
            public int   movePenalty;
            public int   gemBonus;
            public int[] starThresholds;
        }

        [Serializable]
        private class TutorialStepJson
        {
            public string topText;
            public string bottomText;
            public int startX, startY, endX, endY;
        }

        [Serializable]
        private class BlobJson
        {
            public string t;   // blob type  key  — see LevelDataKeys.Types
            public string c;   // color      key  — see LevelDataKeys.BlobColors
            public string s;   // size       key  — see LevelDataKeys.BlobSizes
            public string tc;  // trail color key — not stored in ScriptableObject yet
            public int    x, y;
        }

        [Serializable]
        private class TileJson
        {
            public string t;   // tile type key — see LevelDataKeys.Types
            public int    x, y;
        }
    }
}
#endif
