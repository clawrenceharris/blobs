using System.Collections;

public abstract class State<TStateManager> : IState  
{
    protected TStateManager context;
    public State(TStateManager context)
    {
        this.context = context;
    }
    public abstract void EnterState();

    public abstract void UpdateState();
    public abstract void ExitState();
    
}