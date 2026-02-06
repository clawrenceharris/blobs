using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Level_", menuName = "Scriptable Objects/Level Data", order = 1)]
public class LevelData : ScriptableObject
{
    public int LevelNumber;
    public int Width;
    public int Height;
    public Scoring Scoring;
    public bool IsTutorial;
    [NonReorderable]
    public List<TutorialStep> TutorialSteps = new();
    public List<BlobSpawnData> Blobs;
    public List<TileSpawnData> Tiles;
    public int MinMoves;

    [NonReorderable]
    public List<LaserLink> LaserLinks;

    public string LevelName;
    
}

public class LaserLink {
    public string idA;
    public string idB;
    public string color;
}

[Serializable]
public class Scoring
{
    public int BaseScore;
    public int MovePenalty;

    [NonReorderable]
    [Tooltip("The thresholds for the star rewards. The first value is the threshold for the first star, the second value is the threshold for the second star, etc.")]
    public int[] StarThresholds;
    public int GemBonus;
    public int UndoPenalty;

}


[Serializable]
public class TutorialStep
{
    public string topText;
    public string bottomText;
    public int startX;
    public int startY;
    public int endX;
    public int endY;
}


[System.Serializable]
public class TileJSON
{
    public string type;
    public int x;
    public int y;
    public string color;

}

[Serializable]
public class BlobJSON
{
    public string type;
    public string color;
    public int x;
    public int y;
    public string trailColor;
    public int size;

}

[Serializable]
public class StringKeyValue
{
    public string Key;
    public string Value;
}

[Serializable]
public class TileSpawnData
{
    public Vector2Int GridPosition;
    public TileType Type = TileType.Normal;
    [Tooltip("Optional type-specific data (e.g. id, color for Laser)")]
    public List<StringKeyValue> Properties = new();
}

[Serializable]
public class BlobSpawnData
{
    public Vector2Int GridPosition;
    public BlobType Type = BlobType.Normal;
    public BlobColor Color = BlobColor.Pink;
    [Tooltip("Optional: Size for certain blob types")]
    public BlobSize Size = BlobSize.Normal;
    [Tooltip("Optional: Trail color for TrailBlob")]
    public BlobColor TrailColor = BlobColor.Pink;
    [Tooltip("Optional: Type-specific keys (color, size, trailColor, etc.) for JSON-style overrides")]
    public List<StringKeyValue> Properties = new();
}




