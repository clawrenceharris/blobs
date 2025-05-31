using System.Collections;

public interface IState{
    void EnterState();
    void UpdateState();
    void ExitState();
}