using UnityEngine;
using Blobs.Visuals;
[RequireComponent(typeof(TargetBlobVisuals))]
public class TargetBlobView : ColorBlobView
{


    public override void SetColor(BlobColor color)
    {
        TargetBlobVisuals visuals = (TargetBlobVisuals)Visuals;
        visuals.FlagSprite.color = ColorSchemeManager.FromBlobColor(color);
        visuals.FlagPoleFinialSprite.color = ColorSchemeManager.FromBlobColor(color);
    }
   
}