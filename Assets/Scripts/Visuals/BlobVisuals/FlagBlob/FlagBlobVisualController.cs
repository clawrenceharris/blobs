using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlagBlobVisualController : ColorBlobVisualController
{



    protected override void InitSprite()
    {

        Visuals.flagSprite.color = ColorSchemeManager.FromBlobColor(blob.Color);
        Visuals.flagPoleSprite.color = ColorSchemeManager.FromBlobColor(blob.Color);
        base.InitSprite();
    }
}
