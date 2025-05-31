using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Type;
public class Laser : Tile
{
    public BlobColor color {get; private set;}
    public bool isOff {get; private set;}
    public Position position {get; private set;}
    public Direction direction {get; private set;}

    public override TileType TileType => TileType.Laser;

    public void Init(Position position, BlobColor color, Direction direction){
        this.position = position;
        this.color = color;
        this.direction = direction;
        this.isOff = false;
        transform.position = new Vector3(position.x, position.y);
        InitDisplayController();
        SwitchBlob.onSwitchedOff += OnSwitchedOff;
        SwitchBlob.onSwitchedOn += OnSwitchedOn;
    }
    
     private void InitDisplayController(){
        
        LaserVisualController visualController = new();
        visualController.Init(this);
    }

    private void OnSwitchedOff(SwitchBlob switchBlob){
        if(switchBlob.Color == this.color){
            isOff = true;
            StartCoroutine(GetVisualController<LaserVisualController>().BlinkOff(4));
        }
    }

    private void OnSwitchedOn(SwitchBlob switchBlob){
        Debug.Log("switch blob color:" + this.color);
        if(switchBlob.Color == this.color){
            isOff = false;
            StartCoroutine(GetVisualController<LaserVisualController>().BlinkOn(4));
        }
    }
}
