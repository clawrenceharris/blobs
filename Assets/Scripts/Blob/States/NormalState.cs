using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NormalState : BlobBaseState
{

    public NormalState(BlobStateMachine context):base(context){}
    
    public override void EnterState(){

        BlobState = BlobState.Normal;        
    }

    public override BlobBaseState UpdateState(){
        // if(Context.Blob.AmountMergedInColumn >= 2){
        //     return Context.HStripeState;
        // }

        // else if(Context.Blob.AmountMergedInRow >= 2){
        //    return Context.VStripeState;
        // }
        
        return this;
    }

    public override void UndoState(){

    }

    
}
