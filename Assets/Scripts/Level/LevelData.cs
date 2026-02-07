using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Level_", menuName = "Scriptable Objects/Level Data", order = 1)]
public class LevelData : ScriptableObject
{
    public int LevelNumber;
    public string LevelName;

    public int Width;
    public int Height;
    public Scoring Scoring;
    public bool IsTutorial;
    [NonReorderable] public TutorialStep[] TutorialSteps;
    public List<BlobSpawnData> Blobs;
    public List<TileSpawnData> Tiles;
    public int MinMoves;

    [NonReorderable] public List<LaserLink> LaserLinks;

    
}

public class LaserLink {
    public string IdA;
    public string IdB;
    public string Color;
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
    [TextArea(3, 10)] public string TopText;
    [TextArea(3, 10)] public string BottomText;
    public int StartX;
    public int StartY;
    public int EndX;
    public int EndY;
}


[Serializable]
public class TileSpawnData : SpawnData
{
    public TileType Type = TileType.Normal;
}

[Serializable]
public class BlobSpawnData :SpawnData
{
    public BlobType Type = BlobType.Normal;
    public BlobColor Color = BlobColor.Pink;
    [Tooltip("Optional: Size for certain blob types")]
    public BlobSize Size = BlobSize.Normal;
}




