using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetTileVisualController : TileVisualController
{


    private new TargetTile tile;
    private new TargetTileVisuals Visuals;
    public override void Init(BlobsGameObject bOject)
    {
        base.Init(tile);
        tile = tile.GetComponent<TargetTile>();
        Visuals = tile.GetComponent<TargetTileVisuals>();


    }


    protected override void InitSprite()
    {

        base.InitSprite();
        
        
        //make the checkered background game objects
        Color color = ColorSchemeManager.FromBlobColor(tile.Color);
        color.a = 0.8f;
        Visuals.whiteBg.color = color;

        color = Color.black;
        color.a = 0.5f;
        Visuals.checkersSprite.color = color;
        

        
    }
}
