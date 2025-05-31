using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RemoveAction : IAction
{
    
    private int _initialSize;

    public delegate void OnBlobRemoved(Blob blob);
    public static event OnBlobRemoved onBlobRemoved;
    public delegate void OnBlobRemoveUndone(Blob blob);
    public static event OnBlobRemoveUndone onBlobRemoveUndone;
    private Blob blob;
   
    public float waitTime {get; private set;} = 0.4f;
    public RemoveAction(Blob blob)
    {
        this.blob = blob;
        _initialSize = blob.Size;

    }

    public void Execute(Board board)
    {

        blob.DoMerge(-blob.Size);
        onBlobRemoved?.Invoke(blob);
    }
    
    public void Undo(Board board)
    {

        blob.DoMerge(_initialSize);
        onBlobRemoveUndone?.Invoke(blob);


    }


}
