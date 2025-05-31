using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
public abstract class Blob : BlobsGameObject
{
    public int AmountMergedInRow { get; protected set; } = 0;
    public int AmountMergedInColumn { get; protected set; } = 0;

   
    public int MaxSize { get; protected set; } = 4;
    protected List<MergeAction> merges = new();
    public Type.BlobType BlobType { get; protected set; }
    public bool Disabled = false;
    public int Size;
    public BlobColor Color{ get; protected set; }


    public BlobStateMachine Context { get; private set; }
   

    private void Awake() => Context = GetComponent<BlobStateMachine>();

    public override void Init(Position position)
    {
        Position = position;

    }
    public virtual void Init(Position position, BlobColor color)
    {
        Position = position;
        Color = color;
        Size = 5;
    }
    public virtual void Init(Position position, BlobColor color, int size)
    {
        Position = position;
        Color = color;
        Size = size;
    }
    public virtual void Init(Position position, BlobColor color, BlobColor trailColor, int size)
    {
        Position = position;
        Color = color;
        Size = size;
    }

    

    public virtual void DoMove(Position position)
    {
        Position = position;
    }

    public abstract void DoMerge(int amount = 0);
    public abstract bool CanMoveTo(Blob blobToMoveTo);

    public virtual void Remove(){
        gameObject.SetActive(false);
        
    }
   
    public void ResetAmountMergedInCoulmn(){
        AmountMergedInColumn = 0;
    }

    public void ResetAmountMergedInRow(){
        AmountMergedInColumn = 0;
    }
}
