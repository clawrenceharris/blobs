
public interface IStateMachine
{
    IState CurrentState { get; }
    void Initialize(IState initialState);
    void SetState(IState state);
    void Update();
    event System.Action<IState> OnStateChanged;
}