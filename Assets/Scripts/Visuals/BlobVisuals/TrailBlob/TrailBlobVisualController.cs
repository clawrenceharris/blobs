using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrailBlobVisualController : NormalBlobVisualController

{
    private new TrailBlob blob;
    
    protected new TrailBlobVisuals Visuals;

    public override void Init(BlobsGameObject bObject)
    {

        base.Init(bObject);
        Visuals = bObject.GetComponent<TrailBlobVisuals>();

    }

    protected override void InitSprite(){
        base.InitSprite();
        Visuals.trailSprite.color = ColorSchemeManager.FromBlobColor(blob.TrailColor);

    }




}
