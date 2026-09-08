using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor;
public class LevelLoader : MonoBehaviour
{

    [Header("Level Data")]
    [SerializeField] private LevelData[] _allLevels;
    
    public static LevelData[] AllLevels { get; private set; } = Array.Empty<LevelData>();
    public static int TotalLevelCount => AllLevels.Length;
    /// <summary>
    /// Static reference to pass selected level data to the gameplay scene.
    /// </summary>
    public static LevelData SelectedLevelData { get; private set; }


    private void Awake()
    {
        AllLevels = LoadAllLevels();
    }
    public static LevelData SelectLevel(int levelIndex)
    {
        if (levelIndex < 0 || levelIndex >= AllLevels.Length)
        {
            Debug.LogWarning($"[LevelLoader] Invalid level index: {levelIndex}");
            return null;
        }

        SelectedLevelData = AllLevels[levelIndex];

        return SelectedLevelData;
    }
   

    /// <summary>
    /// Load levels from ScriptableObject assets in Resources/Levels first;
    /// if none found, fall back to JSON files under Assets/Levels.
    /// </summary>
    public LevelData[] LoadAllLevels()
    {
        // Attempt to load all LevelData ScriptableObjects in any Resources/Levels subfolder
        List<LevelData> assetsList = new();
        LevelData[] resourcesRoot = Resources.LoadAll<LevelData>("Levels");
        assetsList.AddRange(resourcesRoot);

        // Also search deeper, e.g. Resources/Levels/* (Unity limitation workaround)
        LevelData[] deepAssets = Resources.LoadAll<LevelData>("");
        foreach(var asset in deepAssets) {
            if (asset != null && AssetDatabase.GetAssetPath(asset).Contains("/Levels/") && !assetsList.Contains(asset)) {
                assetsList.Add(asset);
            }
        }
#if UNITY_EDITOR
        // If still empty, try Assets/Levels in Editor mode via AssetDatabase (not available in build)
        if (assetsList.Count == 0)
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:LevelData", new[] { "Assets/Levels" });
            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                LevelData level = UnityEditor.AssetDatabase.LoadAssetAtPath<LevelData>(path);
                if (level != null && !assetsList.Contains(level))
                    assetsList.Add(level);
            }
        }
#endif
        LevelData[] assets = assetsList.ToArray();
        if (assets != null && assets.Length > 0)
        {
            System.Array.Sort(assets, (a, b) => a.LevelNumber.CompareTo(b.LevelNumber));
            return assets;
        }
        Debug.LogWarning("No levels found");
        return Array.Empty<LevelData>();
    }

    public static void ClearSelectedLevelData()
    {
        SelectedLevelData = null;
    }
}




