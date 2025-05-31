using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using Unity.VisualScripting;
using UnityEngine;

public class ColorBlobVisualController : BlobVisualController
{
    
    public override void Init(BlobsGameObject bObject){
        base.Init(bObject);
    }
            
    public void OnHStripe(){
       // blob.Visuals.horizontalStripes.enabled = true;
    }

    public void OnVStripe(){
       // blob.Visuals.verticalStripes.enabled = true;
    }

    public void OnTStripe(){
        //blob.Visuals.verticalStripes.enabled = true;
       // blob.Visuals.horizontalStripes.enabled = true;
    }

    public void OnNoStripe(){
        //blob.Visuals.verticalStripes.enabled = false;
       // blob.Visuals.horizontalStripes.enabled = false;
    }


   
    public void OnBlobMerged()
    {

        switch (blob.Size)
        {
            case 0: blob.Remove(); break;
            case 1: 
            case 2: DoMerge(BlobVisuals.smallSize); break;
            case >=3: DoMerge(BlobVisuals.bigSize); break;
            

        }
    }
  








}
