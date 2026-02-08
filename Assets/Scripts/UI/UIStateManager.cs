using Blobs.Core.UI;
using UnityEngine;

public class UIStateManager : MonoBehaviour
{
    private IStateMachine _stateMachine;
    public IState CurrentState => _stateMachine.CurrentState;
    public UIManager UIManager => _uiManager;
    private UIManager _uiManager;

    private void Awake()
    {
        _stateMachine = new StateMachine();
        _uiManager = FindFirstObjectByType<UIManager>();
    }

    private void Update()
    {
        _stateMachine.Update();
    }

    public void ChangeState(IState state)
    {
        _stateMachine.SetState(state);
    }

    public void Initialize(IState initialState)
    {
        _stateMachine.Initialize(initialState);
    }
    
}