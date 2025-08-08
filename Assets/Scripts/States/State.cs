using System.Collections;

public abstract class State : IState
{
    protected StateManager context;
    public State(StateManager context)
    {
        this.context = context;
    }
    public abstract void EnterState();

    public abstract void UpdateState();
    public abstract void ExitState();
    
}