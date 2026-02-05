

using UnityEngine;

[RequireComponent(typeof(TrailBlobVisuals))]
public class TrailBlobView : ColorBlobView
{
   
   
    // The Presenter calls this to link the View to its data Model.
    public override void Initialize(Blob model)
    {
        base.Initialize(model);
        // Configure the visuals based on the data.
        TrailBlob trailBlob = (TrailBlob)model;
        TrailBlobVisuals visuals = (TrailBlobVisuals)Visuals;
        Blobs.Utilities.ColorUtility.ApplyColorsToMaterial(visuals.TrailSprite.material, trailBlob.TrailColor);
        
    }

  
}
