using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _moveCountText;
    [SerializeField] private Button _undoButton;
    public Button UndoButton => _undoButton;
    [SerializeField] private Button _pauseButton;
    public Button PauseButton => _pauseButton;
    public TextMeshProUGUI MoveCountText => _moveCountText;



    private void Start()
    {
        GameManager.OnMoveCountChanged += OnMoveCountChanged;
        GameManager.OnLevelStarted += OnLevelStarted;

    }
    /// <summary>
    /// Update the gameplay score display with animation.
    /// </summary>
    public void UpdateMoves(int moveCount)
    {
        if (_moveCountText == null) return;

        // Simple punch animation
        _moveCountText.text = $"{moveCount}";

        // Kill existing tween on the transform to avoid conflicts
        _moveCountText.transform.DOKill(true);

        // Punch scale effect
        _moveCountText.transform.DOPunchScale(Vector3.one * 0.1f, 0.3f, 2, 1f);
    }

  
   
    private void OnMoveCountChanged(int moveCount)
    {
        UpdateMoves(moveCount);
    }

    private void OnLevelStarted(LevelData level)
    {
        if (level.IsTutorial)
        {
            _undoButton.interactable = false;

        }

    }

    public void UpdateUndoButtonState()
    {

        if (_undoButton != null)
        {
            _undoButton.interactable = MergeInvoker.CanUndo;
        }
    }

    private void OnDestroy()
    {
        GameManager.OnMoveCountChanged -= OnMoveCountChanged;
        GameManager.OnLevelStarted -= OnLevelStarted;
    }

}