using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LevelData
{
    public int levelNum;
    public int width;
    public int height;
    public Scoring scoring;
    public float padding;
    public TutorialStep[] tutorialSteps;
    public Blob[] blobs;
    public Tile[] tiles;
    public int minMoves;
    public List<LaserLink> laserLinks;
}

public class LaserLink {
    public string idA;
    public string idB;
    internal string color;
}

[System.Serializable]
public class Scoring
{
    public int baseScore;
    public int movePenalty;
    public int[] starThresholds;
    public int gemBonus;
}


[System.Serializable]
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

[System.Serializable]
public class BlobJSON
{
    public string type;
    public string color;
    public int x;
    public int y;
    public string trailColor;
    public int size;
}



