using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BlobBaseState
{
    protected BlobStateMachine Context;
    protected Blob Blob;
    public BlobState BlobState { get; protected set; }
    public BlobBaseState(BlobStateMachine context){
        Context = context;
        Blob = context.Blob;
    }
    
    public abstract void EnterState();
    public abstract BlobBaseState UpdateState();
    public abstract void UndoState();

}
