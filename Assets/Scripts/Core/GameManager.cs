
using UnityEngine;
using System;
using Blobs.Core.Merge;
using Blobs.Input;
using System.Linq;

[RequireComponent(typeof(GameStateManager))]
public class GameManager : MonoBehaviour


{
    public static event Action<LevelData> OnLevelStarted;
    private LevelData _startingLevel;
    public bool IsHighscore { get; private set; }
    private GameStateManager _stateManager;
    public static Action<int> OnMoveCountChanged;
    
    private BoardPresenter _board;

    private static ColorScheme _theme;

    public ColorScheme Theme
    {
        get
        {
            if (_theme == null)
            {
                _theme = FindFirstObjectByType<ColorScheme>();
            }
            return _theme;
        }
    }

    public LevelData StartingLevel => _startingLevel;

    private void Awake()
    {
        _board = FindFirstObjectByType<BoardPresenter>();
        _stateManager = GetComponent<GameStateManager>();
    }



    private void Start()
    {
        BoardPresenter.OnMergeAnimationStart += HandleMergeAnimationStart;
        BoardPresenter.OnMergeAnimationComplete += HandleMergeAnimationComplete;
        MergeInvoker.OnMergeExecuted += HandleMergeExecuted;
        _stateManager.OnMoveCountChanged += moveCount => OnMoveCountChanged?.Invoke(moveCount);
        InitializeGame();

    }

    private void HandleMergeExecuted(MergeAction action)
    {
        _stateManager.IncrementMoveCount();
    }

    private void HandleMergeAnimationStart(MergeAction action)
    {
        InputService.Gate.SetEnabled(false);
    }

    private void HandleMergeAnimationComplete(MergeAction action)
    {
        InputService.Gate.SetEnabled(true);
        bool didWin = CheckForWin(_board);
         if (didWin)
        {
            CoroutineHandler.StartStaticCoroutine(_stateManager.Board.AnimateEndTurnSequence(), () =>
            {
                _stateManager.ChangeState(new WinState(_stateManager));

            });

        }
       
    }
    
    private void InitializeGame()
        {
            _stateManager.Reset();
            if (_board == null)
                _board = FindFirstObjectByType<BoardPresenter>();

            // Check if level data was passed from Main Menu
            
            var startingLevel = LevelLoader.SelectedLevelData;
            
            LevelLoader.ClearSelectedLevelData();


        if (startingLevel != null)
        {
            StartLevel(startingLevel);
        }
        else
        {
            StartLevel(LevelLoader.AllLevels[0]);
        }

            // Play gameplay BGM
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayBGM("gameplay");

            Debug.Log("[GamePresenter] Game initialized");
        }
    /// <summary>
    /// Checks the board state for a win.
    /// </summary>
    public bool CheckForWin(IBoardPresenter board)
    {
        // There must be no clearable Blobs on the board to win.
        var clearableBlobsCount = board.GetAllBlobs().Count(b => b.Model is IClearable);
        if (clearableBlobsCount == 0)
        {
            return true;
        }
        return false;

    }
    public void StartLevel(LevelData level)
    {
        _startingLevel = level;
        _board.Initialize(level);


        if (level.IsTutorial)
        {
            _stateManager.ChangeState(new TutorialState(_stateManager));
        }
        else
        {
            _stateManager.ChangeState(new PlayingState(_stateManager));
        }

        OnLevelStarted?.Invoke(_startingLevel);
        

        

    }   
}
