
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using UnityEngine.UI;
using System.Collections;
using System.Linq;
using Blobs.Input;
using Blobs.Core.Merge;

public class LevelManager : MonoBehaviour


{
    public int LevelNum;
    public LevelData Level { get; private set; }
    public static int MoveCount { get; private set; } = 0;
    public bool IsHighscore { get; private set; }
    private LevelStateManager _stateManager;

    private WinConditionSystem _winConditionSystem;
    public bool IsTutorial
    {
        get
        {
            return Level != null && Level.TutorialSteps.Length > 0;
        }
    }
    public BoardPresenter Board { get; private set; }

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

    public TutorialPresenter Tutorial { get; private set; }

    private void Awake()
    {

        Tutorial = FindFirstObjectByType<TutorialPresenter>();
        Board = FindFirstObjectByType<BoardPresenter>();
        _stateManager = FindFirstObjectByType<LevelStateManager>();
        LevelLoader.LoadAllLevels();
    }



    private void Start()
    {

        StartLevel(LevelNum);
        BoardModel.OnBlobMoved += OnBlobMoved;
        BoardPresenter.OnMergeStart += HandleMergeStart;
        BoardPresenter.OnMergeComplete += HandleMergeComplete;

    }

    private void HandleMergeStart(MergeAction action)
    {
        _stateManager.ChangeState(new AnimationState(_stateManager));
    }

    private void HandleMergeComplete(MergeAction action)
    {
        _stateManager.ChangeState(new PlayingState(_stateManager));
        bool didWin = _winConditionSystem.CheckForWin(Board.Model);
        if (didWin)
        {
            _stateManager.ChangeState(new LevelEndedState(_stateManager));
            Tutorial.StopTutorial();
            CoroutineHandler.StartStaticCoroutine(Board.AnimateEndTurnSequence(), () =>
            {
                StartLevel(++LevelNum);

            });
        }
    }


    public void StartLevel(int levelNum)
    {

        Level = LevelLoader.Levels.ElementAtOrDefault(levelNum - 1);
        if (Level == null) return;
        MoveCount = 0;

        Board.Initialize(Level);
        _winConditionSystem = new WinConditionSystem();
        _stateManager.ChangeState(new PlayingState(_stateManager));
       
        if (Level.IsTutorial)
        {
            Tutorial.StartTutorial(Board, Level.TutorialSteps);
        }

        

    }

    

    private void OnBlobMoved(Blob blob, Vector2Int from, Vector2Int to)
    {
        MoveCount++;
    }



   
}
