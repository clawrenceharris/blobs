using UnityEngine;
[RequireComponent(typeof(FlagBlobVisuals))]
public class FlagBlobView : BlobView
{
    public override void Initialize(Blob blob)
    {
        base.Initialize(blob);
        FlagBlobVisuals visuals = (FlagBlobVisuals)Visuals;
        visuals.FlagSprite.color = ColorSchemeManager.FromBlobColor(Model.Color);
        visuals.FlagPoleSprite.color = ColorSchemeManager.FromBlobColor(Model.Color);
    }
}