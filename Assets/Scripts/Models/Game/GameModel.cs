using Blobs.Core;


/// <summary>
/// Pure data model for game state. No Unity dependencies.
/// </summary>
[System.Serializable]
public class GameModel
{
    private GameState _currentState;
    private int _moveCount;
    private int _score;
    private int _levelNumber;

    public GameState CurrentState => _currentState;
    public int MoveCount => _moveCount;
    public int Score => _score;
    public int LevelNumber => _levelNumber;

    public event System.Action<GameState> OnStateChanged;
    public event System.Action<int> OnMoveCountChanged;
    public event System.Action<int> OnScoreChanged;

    public GameModel()
    {
        _currentState = GameState.Playing;
        _moveCount = 0;
        _score = 0;
        _levelNumber = 1;
    }

    public void SetState(GameState state)
    {
        if (_currentState == state) return;
        _currentState = state;
        OnStateChanged?.Invoke(state);
    }

    public void IncrementMoveCount()
    {
        _moveCount++;
        OnMoveCountChanged?.Invoke(_moveCount);
    }

    public void AddScore(int points)
    {
        _score += points;
        OnScoreChanged?.Invoke(_score);
    }

    public void SetLevelNumber(int level)
    {
        _levelNumber = level;
    }

    public void Reset()
    {
        _currentState = GameState.Playing;
        _moveCount = 0;
        _score = 0;
        OnStateChanged?.Invoke(_currentState);
        OnMoveCountChanged?.Invoke(_moveCount);
        OnScoreChanged?.Invoke(_score);
    }
}
