using System;
using UnityEngine;
public class StateMachine : IStateMachine
{



    public IState CurrentState { get; private set; }
    public static event Action<IState> OnStateExit;

    public static event Action<IState> OnStateEnter;
    
    public void Initialize(IState initialState)
    {
        SetState(initialState);

    }

    public void SetState(IState newState)
    {
        CurrentState?.ExitState();
        OnStateExit?.Invoke(CurrentState);

        newState?.EnterState();
        OnStateEnter?.Invoke(newState);

        CurrentState = newState;

    }


    public void Update()
    {
        CurrentState?.UpdateState();
    }
}
