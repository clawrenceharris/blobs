

using System;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(TrailBlobVisuals))]
public class TrailBlobView : ColorBlobView
{
   
   
    // The Presenter calls this to link the View to its data Model.
    public override void Setup(Blob model)
    {
        base.Setup(model);
        // Configure the visuals based on the data.
        TrailBlob trailBlob = (TrailBlob)model;
        TrailBlobVisuals visuals = (TrailBlobVisuals)Visuals;
        ColorUtils.ApplyColorsToMaterial(visuals.TrailSprite.material, trailBlob.TrailColor);
        
    }

  
}
