
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

        // Skip win check during tutorials — TutorialState handles its own win transition
        if (_stateManager.CurrentState is TutorialState) return;

        // Prevent duplicate win transitions from spam-clicking
        if (_stateManager.CurrentState is WinState) return;

        bool didWin = CheckForWin(_board);
        if (didWin)
        {
            CoroutineHandler.StartStaticCoroutine(_stateManager.Board.AnimateEndTurnSequence(), () =>
            {
                // Double-check we haven't already transitioned to WinState
                // (another coroutine callback may have beaten us here)
                if (_stateManager.CurrentState is WinState) return;
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

        // If SelectedLevelData is null (e.g. LevelLoader wasn't in the Menu scene),
        // fall back to the index saved in PlayerPrefs.
        if (startingLevel == null)
        {
            int selectedIndex = PlayerPrefs.GetInt("SelectedLevel", 0);
            if (LevelLoader.AllLevels != null && selectedIndex < LevelLoader.AllLevels.Length)
                startingLevel = LevelLoader.AllLevels[selectedIndex];
            else
                startingLevel = LevelLoader.AllLevels[0];
        }

        StartLevel(startingLevel);

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
        // Query the board model directly — GetAllBlobs() includes removed presenters
        // still in the dictionary, so use GetPlayableBlobCount() instead.
        return board.GetPlayableBlobCount() == 0;
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
