using Blobs.Input;
using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    public IState CurrentState => _stateMachine.CurrentState;
    private int _score;
    private int _moveCount;
    public int MoveCount
    {
        get { return _moveCount; }
        set
        {
            _moveCount = value;
            OnMoveCountChanged?.Invoke(_moveCount);
        }
    }
   

       private int _levelNumber;

    public int LevelNumber => _levelNumber;
    private StateMachine _stateMachine;
    public event System.Action<GameState> OnStateChanged;
    public event System.Action<int> OnMoveCountChanged;

    private BoardPresenter _board;

    private GameManager _levelManager;
    private InputGate _inputGate;
    public IBoardPresenter Board => _board;
    public GameManager LevelManager => _levelManager;
    public InputGate InputGate => _inputGate;

    private void Awake()
    {
        _stateMachine = new StateMachine();
        _inputGate = FindFirstObjectByType<InputGate>();
        _levelManager = FindFirstObjectByType<GameManager>();
        _board = FindFirstObjectByType<BoardPresenter>();
        

    }


    public void Initialize(IState initialState)
    {
        _stateMachine.Initialize(initialState);
        MoveCount = 0;
        _levelNumber = 1;
    }

    public void ChangeState(IState state)
    {
        _stateMachine.SetState(state);
    }
    private void Update()
    {
        _stateMachine.Update();
    }
    


    public void IncrementMoveCount()
    {
        MoveCount++;
    }

    
    public void SetLevelNumber(int level)
    {
        _levelNumber = level;
    }

    public void Reset()
    {
        _stateMachine.SetState(null);
        MoveCount = 0;
        
    }




}