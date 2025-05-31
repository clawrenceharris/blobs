using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwitchBlob : ColorBlob
{

    private new SwitchVisualController visualController;
    public bool IsOff {get; private set;}=false;
    public delegate void OnSwitchedOff(SwitchBlob switchBlob);
    public static event OnSwitchedOff onSwitchedOff;
    public delegate void OnSwitchedOn(SwitchBlob switchBlob);
    public static event OnSwitchedOn onSwitchedOn;
    public override void Init(Position position, BlobColor color){
        base.Init(position, color);

        visualController = new SwitchVisualController();
        visualController.Init(this);

    }

    public override void DoMove(Position position){
        visualController.DoMerge(0);
    }
    public override bool CanMoveTo(Blob blobToMoveTo){
        return true;
    }

    public override void DoMerge(int size){
        visualController.OnBlobMerged();
        
        if(IsOff){
            
            IsOff = false;
            onSwitchedOn?.Invoke(this);
            visualController.SwitchOn();
            Debug.Log("Turing On");

        }
        else
        {
            IsOff = true;
            onSwitchedOff?.Invoke(this);
            visualController.SwitchOff();
            Debug.Log("Turing Off");

        }
        
        
        
    }

    protected override void InitVisuals()
    {
        
    }
}
