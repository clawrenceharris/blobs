using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlagBlob : ColorBlob
{
   

    public override void DoMove(Position position){
        GetVisualController<ColorBlobVisualController>().DoMerge(0);
    }

   

    public override bool CanMoveTo(Blob targetBlob){
        if(targetBlob.Color == Color){
            return true;
        }
        return false;
    }

    public override void DoMerge(int size){
         GetVisualController<ColorBlobVisualController>().OnBlobMerged();

    }

    protected override void InitVisuals()
    {
        visualController = new FlagBlobVisualController();
        visualController.Init(this);
    }
}
