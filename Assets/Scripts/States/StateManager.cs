using System;
using UnityEngine;
public class StateMachine : IStateMachine
{



    public IState CurrentState { get; private set; }
    public event Action<IState> OnStateChanged;

    
    public void Initialize(IState initialState)
    {
        SetState(initialState);

    }

    public void SetState(IState newState)
    {
        CurrentState?.ExitState();
        OnStateChanged?.Invoke(CurrentState);

        newState?.EnterState();

        CurrentState = newState;

    }


    public void Update()
    {
        CurrentState?.UpdateState();
    }
}
