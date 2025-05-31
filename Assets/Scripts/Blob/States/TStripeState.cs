using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TStripeState :BlobBaseState
{

    
    public TStripeState(BlobStateMachine context):base(context){}
    public override void EnterState() => BlobState = BlobState.TStripe;

    public override BlobBaseState UpdateState(){
        //Blob.OnTStripe();
        return this;
    }

    public override void UndoState(){

    }

    
}
