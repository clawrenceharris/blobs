
using System;
using UnityEngine;

public abstract class Blob : IBlobModel, IBoardElement, ISizable, IColorable
{
    public string ID { get; private set; }
    public Vector2Int GridPosition { get; set; }
    public BlobType Type { get; protected set; }
    public BlobColor Color { get; set; }
    public virtual BlobMergeBehavior Behavior => new(this);
    public BlobSize Size { get; set; }
    public virtual ColorRule Rule { get; }
    public virtual bool Enabled { get; private set; }

    public Blob(BlobType type, BlobColor color, BlobSize size, Vector2Int position)
    {
        GridPosition = position;
        Type = type;
        Color = color;
        ID = Guid.NewGuid().ToString();
        Size = size;
        Rule = new DifferentColorRule();
        Enabled = true;

    }
    public virtual bool CanMergeWith(Blob targetBlob, MergePlan plan, BoardModel board)
    {
        return Rule.Validate(this, targetBlob, board);
    }
    public void EnableBlob()
    {  
        Enabled = true;  
    }

    public void DisableBlob()
    {
        Enabled = false;
    }
}