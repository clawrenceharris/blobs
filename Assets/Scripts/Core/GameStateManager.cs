using System;
using Blobs.Core.UI;
using Blobs.Input;
using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    public IState CurrentState => _stateMachine.CurrentState;
    public UIManager UIManager => _uiManager;
    private UIManager _uiManager;
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
   
    private IStateMachine _stateMachine;
    public event Action<int> OnMoveCountChanged;

    private BoardPresenter _board;

    private GameManager _gameManager;
    private TutorialPresenter _tutorial;

    public TutorialPresenter Tutorial => _tutorial;

    public IBoardPresenter Board => _board;
    public GameManager GameManager => _gameManager;

    private void Awake()
    {
        _stateMachine = new StateMachine();
        _tutorial = new TutorialPresenter();
        _gameManager = FindFirstObjectByType<GameManager>();
        _board = FindFirstObjectByType<BoardPresenter>();
        _uiManager = FindFirstObjectByType<UIManager>();
    }


    public void Initialize(IState initialState)
    {
        _stateMachine.Initialize(initialState);
        MoveCount = 0;
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

    public void Reset()
    {
        _stateMachine.SetState(null);
        MoveCount = 0;
        
    }

}