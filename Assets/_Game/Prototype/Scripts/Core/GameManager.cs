
using UnityEngine;
using System;
using Blobs.Input;
using System.Linq;

[RequireComponent(typeof(GameStateManager))]
public class GameManager : MonoBehaviour


{
    public static event Action<LevelData> OnLevelStarted;
    private LevelData _currentLevel;
    public bool IsHighscore { get; private set; }
    private GameStateManager _stateManager;
    public static Action<int> OnMoveCountChanged;
    [SerializeField] private LevelData _level;
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

    public LevelData StartingLevel => _currentLevel;

    private void Awake()
    {
        _board = FindFirstObjectByType<BoardPresenter>();
        _stateManager = GetComponent<GameStateManager>();
    }



    private void Start()
    {
        BoardPresenter.OnMergeAnimationStart += OnMergeAnimationStart;
        BoardPresenter.OnMergeAnimationComplete += OnMergeAnimationComplete;
        MergeInvoker.OnMergeExecuted += OnMergeExecuted;
        _stateManager.OnMoveCountChanged += moveCount => OnMoveCountChanged?.Invoke(moveCount);
        InitializeGame();

    }

    private void OnMergeExecuted(ICommand command)
    {
        _stateManager.IncrementMoveCount();
    }

    private void OnMergeAnimationStart(ICommand command)
    {
        InputService.Gate.SetEnabled(false);
    }

    private void OnDestroy()
    {
        BoardPresenter.OnMergeAnimationStart -= OnMergeAnimationStart;
        BoardPresenter.OnMergeAnimationComplete -= OnMergeAnimationComplete;
        MergeInvoker.OnMergeExecuted -= OnMergeExecuted;
    }

    private void OnMergeAnimationComplete(ICommand command)
    {
        InputService.Gate.SetEnabled(true);
        bool didWin = CheckForWin();
         if (didWin)
        {
            CoroutineHandler.StartStaticCoroutine(_stateManager.Board.AnimateEndTurnSequence(), () =>
            {
                ProcessWin();
                MergeInvoker.ClearHistory();
                LevelLoader.SelectLevel(_currentLevel.LevelNumber + 1);
                InitializeGame();

            });

        }
       
    }
    private void ProcessWin()
    {
        // TODO: Save player data for this level 
        _stateManager.ChangeState(new WinState(_stateManager));
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
        else if (LevelLoader.AllLevels.Length > 0)
        {
            StartLevel(_level);
        }
        else
        {
            Debug.LogError("No levels found");
            return;
        }

            // Play gameplay BGM
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayBGM("gameplay");

            Debug.Log("[GamePresenter] Game initialized");
        }
    /// <summary>
    /// Checks the board state for a win.
    /// </summary>
    public bool CheckForWin()
    {
        if (_board == null)
        {
            return false;
        }
        // There must be no clearable Blobs on the board to win.
        var clearableBlobsCount = _board.GetBlobsOnBoard().Count(b => b.Model.Type.IsClearable());
        if (clearableBlobsCount == 0)
        {
            return true;
        }
        return false;

    }
    public void StartLevel(LevelData level)
    {
        _currentLevel = level;
        _board.Initialize(level);


        if (level.IsTutorial)
        {
            _stateManager.ChangeState(new TutorialState(_stateManager));
        }
        else
        {
            _stateManager.ChangeState(new PlayingState(_stateManager));
        }

        OnLevelStarted?.Invoke(_currentLevel);
        

        

    }   
}
