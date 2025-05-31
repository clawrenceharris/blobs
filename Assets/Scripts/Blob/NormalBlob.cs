using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NormalBlob : ColorBlob
{
    public override void Init(Position position, BlobColor color, int size)
    {
        base.Init(position, color, size);
        
        visualController = new NormalBlobVisualController();
        visualController.Init(this);
    }

    protected override void InitVisuals()
    {
        visualController = new NormalBlobVisualController();
        visualController.Init(this);
    }
}
