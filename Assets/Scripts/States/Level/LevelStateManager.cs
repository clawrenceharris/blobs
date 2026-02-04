using Blobs.Input;
using UnityEngine;

public class LevelStateManager : MonoBehaviour
{

    private StateMachine _stateMachine; 
    public IBoardPresenter Board { get; private set; }
    public LevelManager LevelManager { get; private set; }
    public InputGate InputGate { get; private set; }

    private void Awake()
    {
        _stateMachine = new StateMachine();
        InputGate = FindFirstObjectByType<InputGate>();
        LevelManager = FindFirstObjectByType<LevelManager>();
        Board = FindFirstObjectByType<BoardPresenter>();

    }
    private void Start()
    {
        _stateMachine.SetState(null);
    }

    public void ChangeState(IState state)
    {
        _stateMachine.SetState(state);
    }
    private void Update()
    {
        _stateMachine.Update();
    }




}