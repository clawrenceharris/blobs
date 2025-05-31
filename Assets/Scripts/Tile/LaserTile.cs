using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Type;
public class LaserTile : Tile
{
    private LaserTile otherLaser;
    public BlobColor color {get; private set;}
    public bool isOff {get; private set;}

    public override TileType TileType => TileType.Laser;

    private static LaserManager laserManager;
    public List<Laser> lasers = new List<Laser>();
    public Dictionary<string, BlobColor> colors;
    

    public Dictionary<string, bool> isOffDict = new Dictionary<string, bool>(){
        {"Top", false},
        {"Right", false},
        {"Bottom", false},
        {"Left", false}
    };


    void Awake(){
        Board.OnBoardCreated += OnBoardCreated;   
    }

     public override void Init(Position position){
        base.Init(position);
    }

    private void OnEnable() {
        SwitchBlob.onSwitchedOff += OnSwitchedOff;
        SwitchBlob.onSwitchedOn += OnSwitchedOn;
    }

    private void OnBoardCreated(Board board){
        if(laserManager == null){
            laserManager = FindFirstObjectByType<LaserManager>();
            laserManager.Init(board);
            laserManager.CreateLasers();

        }
        
    }

    private void OnSwitchedOff(SwitchBlob switchBlob){
        foreach(var color in colors){
            if(color.Value == switchBlob.Color){
                isOffDict[color.Key] = true;
            }
        }
    }

    private void OnSwitchedOn(SwitchBlob switchBlob){
       //key is the string
        //value is the color
       
        foreach(var color in colors){
            if(color.Value == switchBlob.Color){
                isOffDict[color.Key] = false;
            }
        }
       
    }

    public bool IsLaserOnTile(Tile tile){
        if(IsTileOnSameColumn(tile) || IsTileOnSameRow(tile))
            return laserManager.IsLaserOnTile(tile);
        else 
            return false;
    }

    private bool IsTileOnSameRow(Tile tile){
        return tile.Position.y == this.Position.y;
    }

    private bool IsTileOnSameColumn(Tile tile){
 
        return tile.Position.x == this.Position.x;
    }

    

    public void Link(LaserTile laser){
        this.otherLaser = laser;
    }

    
}

