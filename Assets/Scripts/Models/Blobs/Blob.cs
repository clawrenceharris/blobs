
using System;
using UnityEngine;


public abstract class Blob : IBlobModel, IBoardElement, ISizable, IColorable
{
    public string ID { get; protected set; }
    public Vector2Int GridPosition { get; set; }
    public BlobType Type { get; protected set; }
    public BlobColor Color { get; set; }
    public BlobSize Size { get; set; }
    public bool Enabled { get; private set; }
    public bool IsSelected { get; private set; }
    public virtual IMergeRule MatchRule => new BasicMergeRule();

    public virtual IMoveBehavior MoveBehavior => new NormalBlobMoveBehavior();

    public Blob(BlobType type, BlobColor color, BlobSize size, Vector2Int position, string id = null)
    {
        GridPosition = position;
        Type = type;
        Color = color;
        ID = id ?? Guid.NewGuid().ToString();
        Size = size;
        Enabled = true;
    }

    public void EnableBlob()
    {
        Enabled = true;
    }

    public void DisableBlob()
    {
        Enabled = false;
    }
    public float GetScaleFromBlobSize()
    {
        return Size switch
        {
            BlobSize.Small => 0.65f,
            BlobSize.Big => 1.4f,
            _ => 1,
        };
    }

    public void Select()
    {
        IsSelected = true;
    }
    public void Deselect()
    {
        IsSelected = false;
    }

    public override string ToString()
    {
        string str = "";
        str += Type + " ";

        str += "(" + GridPosition.x + ", " + GridPosition.y + ")";
        return str;
    }

}