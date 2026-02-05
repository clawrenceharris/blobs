using UnityEngine;
using Blobs.Core;
using Blobs.Services;
using Blobs.Commands;
using Blobs.Core.Merge;


/// <summary>
/// Presenter for game state logic.
/// Manages game state, scoring, and win conditions.
/// Subscribes to MoveService events for game progression (SRP compliance).
/// </summary>
public class GamePresenter : MonoBehaviour, IGamePresenter
    {
        [Header("References")]
        [SerializeField] private BoardPresenter _board;
        [SerializeField] private LevelData startingLevel;

        private GameModel _model;
        private int undoCount = 0;

        public GameState CurrentState => _model?.CurrentState ?? GameState.Playing;
        public int MoveCount => _model?.MoveCount ?? 0;
        public int Score => _model?.Score ?? 0;

        public event System.Action<GameState> OnGameStateChanged;
        public event System.Action<int> OnMoveCountChanged;
        public event System.Action<int> OnScoreChanged;

        private void Awake()
        {
            _model = new GameModel();

            // Subscribe to model events
            _model.OnStateChanged += state => OnGameStateChanged?.Invoke(state);
            _model.OnMoveCountChanged += count => OnMoveCountChanged?.Invoke(count);
            _model.OnScoreChanged += score => OnScoreChanged?.Invoke(score);
            _board = FindFirstObjectByType<BoardPresenter>();

        }

        private void Start()
        {
            InitializeGame();
        }

    
    private void HandleUndoPerformed(MergeAction action)
    {
        undoCount++;
        Debug.Log($"[GamePresenter] Undo count: {undoCount}");
        UpdateScoreUI();
    }
        
        private void HandleMergeExecuted(IBlobPresenter source, IBlobPresenter target)
        {
            // Increment move count
            IncrementMoveCount();

            // Check win condition
            CheckWinCondition();
        }

        private void OnDestroy()
        {
            

           
            MergeInvoker.OnMergeUndone -= HandleUndoPerformed;
            
        }

        public void InitializeGame()
        {
            _model.Reset();
            undoCount = 0;

            // Check if level data was passed from Main Menu
            if (MainMenuController.SelectedLevelData != null)
            {
                startingLevel = MainMenuController.SelectedLevelData;
            }

            if (startingLevel != null && _board != null)
            {
                _board.Initialize(startingLevel);
                _model.SetLevelNumber(startingLevel.LevelNumber);
            }

            // Play gameplay BGM
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayBGM("gameplay");

            Debug.Log("[GamePresenter] Game initialized");
        }

        public void StartGame()
        {
            SetGameState(GameState.Playing);
        }

        public void PauseGame()
        {
            if (CurrentState == GameState.Playing)
            {
                SetGameState(GameState.Paused);
                Time.timeScale = 0f;
            }
        }

        public void ResumeGame()
        {
            if (CurrentState == GameState.Paused)
            {
                SetGameState(GameState.Playing);
                Time.timeScale = 1f;
            }
        }

        public void RestartGame()
        {
            Time.timeScale = 1f;
            InitializeGame();
        }

        public void SetGameState(GameState state)
        {
            _model.SetState(state);
            Debug.Log($"[GamePresenter] State changed to: {state}");
        }

        public void CheckWinCondition()
        {
            if (_board == null) return;

            int playableCount = _board.GetPlayableBlobCount();

            if (playableCount <= 1)
            {
                SetGameState(GameState.Win);
                CalculateFinalScore();
                Debug.Log("[GamePresenter] 🎉 YOU WIN!");
            }
        }

        public void IncrementMoveCount()
        {
            _model.IncrementMoveCount();
            UpdateScoreUI();
        }

        public void AddScore(int points)
        {
            _model.AddScore(points); // Legacy, handled by CalculateCurrentScore now
        }

        private void UpdateScoreUI()
        {
            if (startingLevel == null) return;
            
            int currentScore = CalculateCurrentScore();
            UIManager.Instance?.UpdateScore(currentScore);
        }

        private int CalculateCurrentScore()
        {
            if (startingLevel == null) return 0;
            
            int baseScore = startingLevel.Scoring.BaseScore;
            int movePenalty = startingLevel.Scoring.MovePenalty * MoveCount;
            int undoPenaltyTotal = startingLevel.Scoring.UndoPenalty * undoCount;
            int score = Mathf.Max(0, baseScore - movePenalty - undoPenaltyTotal);
            return score;
        }

        private void CalculateFinalScore()
        {
            if (startingLevel == null) return;

            int finalScore = CalculateCurrentScore();
            _model.AddScore(finalScore);
            Debug.Log($"[GamePresenter] Final Score: {finalScore}");

            // Calculate stars based on undo count
            int stars = CalculateStars();

            // Save progress
            int levelIndex = startingLevel.LevelNumber - 1; // levelNumber is 1-indexed
            LevelProgressManager.SetStars(levelIndex, stars);
            Debug.Log($"[GamePresenter] Saved {stars} stars for level {startingLevel.LevelNumber}");

            // Show win panel
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowWinPanel(stars, finalScore);
            }
        }

        private int CalculateStars()
        {
            // Star calculation based purely on undo count
            // 0 undos = 3 stars
            // 1-2 undos = 2 stars
            // 3-4 undos = 1 star
            // 5+ undos = 0 stars
            
            if (undoCount == 0)
                return 3;
            else if (undoCount <= 2)
                return 2;
            else if (undoCount <= 4)
                return 1;
            else
                return 0;
        }

        public void LoadLevel(LevelData level)
        {
            startingLevel = level;
            RestartGame();
        }

}
