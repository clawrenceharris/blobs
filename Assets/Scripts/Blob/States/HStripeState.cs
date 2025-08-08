using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HStripeState : BlobBaseState
{
    
    
    public HStripeState(BlobStateMachine context):base(context){}
    
    public override void EnterState() => BlobState = BlobState.HStripe;
    

    public override BlobBaseState UpdateState()
    {
        // if(Context.Blob.AmountMergedInColumn >= 2 && Context.Blob.AmountMergedInRow >= 2)
        //     return Context.TStripeState;
        // //Blob.OnHStripe();
        return this;
        
    }

    public override void UndoState(){

    }

   
}
