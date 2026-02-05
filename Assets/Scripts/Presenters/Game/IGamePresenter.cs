

/// <summary>
/// Interface for game presenter (game state management)
/// </summary>
public interface IGamePresenter
{
    GameState CurrentState { get; }
    int MoveCount { get; }
    int Score { get; }
    
    // Game lifecycle
    void InitializeGame();
    void StartGame();
    void PauseGame();
    void ResumeGame();
    void RestartGame();
    
    // State management
    void SetGameState(GameState state);
    void CheckWinCondition();
    
    // Scoring
    void IncrementMoveCount();
    void AddScore(int points);
    
    // Events
    event System.Action<GameState> OnGameStateChanged;
    event System.Action<int> OnMoveCountChanged;
    event System.Action<int> OnScoreChanged;
}
