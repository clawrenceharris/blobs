
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

    public Blob(BlobType type, BlobColor color, BlobSize size, Vector2Int position)
    {
        GridPosition = position;
        Type = type;
        Color = color;
        ID = Guid.NewGuid().ToString();
        Size = size;
        Rule = new DifferentColorRule();

    }
    public virtual bool CanMergeWith(Blob targetBlob, MergePlan plan, BoardLogic board)
    {
        return Rule.Validate(this, targetBlob, board);       
    }
}