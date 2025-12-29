using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LevelData
{
    public int LevelNum;
    public int Width;
    public int Height;
    public Scoring Scoring;
    public bool IsTutorial;
    public TutorialStep[] TutorialSteps;
    public Blob[] Blobs;
    public Tile[] Tiles;
    public int MinMoves;
    public List<LaserLink> LaserLinks;
}

public class LaserLink {
    public string idA;
    public string idB;
    public string color;
}

[Serializable]
public class Scoring
{
    public int baseScore;
    public int movePenalty;
    public int[] starThresholds;
    public int gemBonus;
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




