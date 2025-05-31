using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileVisuals : Visuals
{

    public Sprite cornerRight;
    public Sprite cornerTop;
    public Sprite cornerSingle;
    public Sprite cornerTopAndRight;
    public Sprite leftEdgeBottom;
    public Sprite leftEdgeBottomAndTop;
    public Sprite bottomEdgeLeft;
    public Sprite bottomEdgeLeftAndRight;
    public  SpriteRenderer tileSprite;
    public SpriteRenderer edgeSprite;
    public SpriteRenderer borderSprite;
   

    public static TileVisuals Instance;
    private void Awake()
    {
        Instance = this;
    }

}


