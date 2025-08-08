using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VStripeState : BlobBaseState
{    
    public VStripeState(BlobStateMachine context):base(context){}
    public override void EnterState() => BlobState = BlobState.VStripe;



    public override BlobBaseState UpdateState(){
        //switch state to t stripe if already an h stripe
        // if(Blob.AmountMergedInColumn >= 2 && Blob.AmountMergedInRow >= 2)
        //     this.Context.SwitchState(Context.TStripeState) ;
        // //Blob.OnVStripe();
        return this;

    }

    public override void UndoState(){
        
    }

  
}
