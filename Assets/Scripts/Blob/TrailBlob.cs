using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrailBlob : ColorBlob
{
    public BlobColor TrailColor{get; private set;}
    public override void Init(Position position, BlobColor color, BlobColor trailColor, int size)
    {
        base.Init(position, color, trailColor, size);
        visualController = new TrailBlobVisualController();
        visualController.Init(this);

       

    }

    protected override void InitVisuals()
    {
        visualController = new TrailBlobVisualController();
        visualController.Init(this);
    }
}
