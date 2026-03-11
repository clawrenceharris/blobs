

using UnityEngine;
using Blobs.Visuals;                               
[RequireComponent(typeof(TrailBlobVisuals))]
public class TrailBlobView : ColorBlobView
{


    public override void SetColor(BlobColor color)
    {
        var visuals = GetVisuals<TrailBlobVisuals>();
        var trailBlob = GetModel<TrailBlob>();

        Blobs.Utilities.ColorUtility.ApplyColorsToMaterial(visuals.TrailSprite.material, trailBlob.TrailColor);
        base.SetColor(color);

    }

  
}
