using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlobStateMachine : MonoBehaviour
{
    
   
    public BlobBaseState NormalState {get; private set;}
    public BlobBaseState HStripeState { get; private set;}
    public BlobBaseState VStripeState { get; private set; }
    public BlobBaseState TStripeState { get; private set; }
   
    public Blob Blob {get; protected set;}
    public BlobBaseState CurrentState {get; private set;}

    
    void Awake()
    {
        Blob = GetComponent<Blob>();
        NormalState = new NormalState(this);
    } 
    

    void Start(){
        CurrentState = NormalState;
        CurrentState.EnterState();

    }
    
    public void SwitchState(BlobBaseState state){
        CurrentState = state;
        CurrentState.EnterState();
    }

    void Update(){
        CurrentState = CurrentState.UpdateState();
        
        
    }
}
