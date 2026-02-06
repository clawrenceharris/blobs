
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using UnityEngine.UI;
using System.Collections;
using System.Linq;
using Blobs.Input;
using Blobs.Core.Merge;
using Blobs.Core;

[RequireComponent(typeof(GameStateManager))]
public class GameManager : MonoBehaviour


{
    public int LevelNum;
    private LevelData _startingLevel;
    public bool IsHighscore { get; private set; }
    private GameStateManager _stateManager;

    private WinConditionSystem _winConditionSystem;
    
    private BoardPresenter _board;

    private static ColorScheme _theme;
    private int _undoCount;

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

    private TutorialPresenter _tutorial;

    private void Awake()
    {

        _tutorial = FindFirstObjectByType<TutorialPresenter>();
        _board = FindFirstObjectByType<BoardPresenter>();
        _stateManager = GetComponent<GameStateManager>();
    }



    private void Start()
    {
        BoardPresenter.OnMergeStart += HandleMergeStart;
        BoardPresenter.OnMergeComplete += HandleMergeComplete;
        InitializeGame();

    }

    private void HandleMergeStart(MergeAction action)
    {
        _stateManager.ChangeState(new AnimationState(_stateManager));
    }

    private void HandleMergeComplete(MergeAction action)
    {
        _stateManager.ChangeState(new PlayingState(_stateManager));
        bool didWin = _winConditionSystem.CheckForWin(_board);
        if (didWin)
        {
            _tutorial.StopTutorial();
            CoroutineHandler.StartStaticCoroutine(_board.AnimateEndTurnSequence(), () =>
            {
                _stateManager.ChangeState(new WinState(_stateManager));

            });
        }
    }
    
    private void InitializeGame()
        {
            _stateManager.Reset();
            _undoCount = 0;
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

    public void StartLevel(LevelData level)
    {
        _startingLevel = level;
        _stateManager.SetLevelNumber(level.LevelNumber);
        _board.Initialize(level);

        _winConditionSystem = new WinConditionSystem();
        _stateManager.ChangeState(new PlayingState(_stateManager));

        // _tutorial.TryStartTutorial(_board, _startingLevel);
        
        

        

    }   
}
