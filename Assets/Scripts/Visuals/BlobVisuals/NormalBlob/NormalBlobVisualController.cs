using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NormalBlobVisualController : ColorBlobVisualController
{
   
    protected override void InitSprite()
    {

        Visuals.SpriteRenderer.color = ColorSchemeManager.FromBlobColor(blob.Color);
    
    }
}
