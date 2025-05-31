using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwitchVisualController : ColorBlobVisualController
{

   

    protected override void InitSprite(){
       Visuals.handleSprite.color = ColorSchemeManager.FromBlobColor(blob.Color);


    }

    public void SwitchOn()
    {
        blob.GetComponentInChildren<SwitchAnimation>().SwitchOn();
    }

    public void SwitchOff()
    {
        blob.GetComponentInChildren<SwitchAnimation>().SwitchOff();
    }
}
