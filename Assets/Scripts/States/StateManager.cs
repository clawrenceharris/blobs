using System;
using System.Collections;
using UnityEngine;
public abstract class StateManager : MonoBehaviour, IStateManager
{


    public LevelManager LevelManager { get; private set; }
    public abstract IState InitialState { get; }

    public IState CurrentState { get; protected set; }
    public static event Action<IState> OnStateExit;

    public static event Action<IState> OnStateEnter;
    protected virtual void Awake()
    {
        LevelManager = FindFirstObjectByType<LevelManager>();
    }
    private void Start()
    {
        SetState(InitialState);

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

    private void OnDestroy()
    {
        StopAllCoroutines();
    }
}
