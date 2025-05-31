using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class BlobVisuals :Visuals
{

    public Sprite red;
    public Sprite pink;
    public Sprite purple;
    public Sprite orange;
    public Sprite darkBlue;
    public Sprite lightBlue;
    public Sprite green;
    public Sprite horizontalStripes;
    public ParticleSystem particles;
    public Sprite verticalStripes;
    public SpriteRenderer handleSprite;
    public SpriteRenderer flagSprite;
    public SpriteRenderer flagPoleSprite;
    public static float smallSize = 0.5f;
    public static float bigSize = 0.8f;
    public static BlobVisuals Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }
}
