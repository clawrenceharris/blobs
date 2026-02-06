using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using UnityEngine.SceneManagement;
using Blobs.Core.Merge;

namespace Blobs.Core
{
    
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Feedback Text")]
        [SerializeField] private TextMeshProUGUI moveCountText;
        

        [Header("Input UI")]
        [SerializeField] private Button undoButton;

        [Header("Win Panel")]
        [SerializeField] private GameObject winPanel;
        [SerializeField] private Image[] winStarImages;
        [SerializeField] private Sprite starFilledSprite;
        [SerializeField] private Sprite starEmptySprite;
        [SerializeField] private TextMeshProUGUI winScoreText;
        [SerializeField] private Button nextLevelButton;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button menuButton;

        [SerializeField] private Button pauseButton;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button retryPauseButton;
        [SerializeField] private Button menuPauseButton;


        private void Awake()
        {
            // Singleton
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
           
        }

        private void Start()
        {
            SetupButtonListeners();
            UpdateUndoButtonState();
            MergeInvoker.OnMergeUndone += HandleMergeUndone;
            MergeInvoker.OnMergeExecuted += HandleMergeExecuted;
            GameManager.OnMoveCountChanged += HandleMoveCountChanged;

        }
        private void HandleMergeExecuted(MergeAction action)
        {
            UpdateUndoButtonState();
        }
        
        private void HandleMergeUndone(MergeAction action)
        {
            UpdateUndoButtonState();
        }

        private void HandleMoveCountChanged(int moveCount)
        {
            UpdateMoves(moveCount);
        }

       

        private void UpdateUndoButtonState()
        {
            if (undoButton != null)
            {
                undoButton.interactable = MergeInvoker.CanUndo;
            }
        }

        private void SetupButtonListeners()
        {
            if (nextLevelButton != null) nextLevelButton.onClick.AddListener(OnNextLevelClicked);
            if (retryButton != null) retryButton.onClick.AddListener(OnRetryClicked);
            if (menuButton != null) menuButton.onClick.AddListener(OnMenuClicked);
            if (undoButton != null) undoButton.onClick.AddListener(OnUndoClicked);

            // Pause buttons
            if (pauseButton != null) pauseButton.onClick.AddListener(OnPauseClicked);
            if (resumeButton != null) resumeButton.onClick.AddListener(OnResumeClicked);
            if (retryPauseButton != null) retryPauseButton.onClick.AddListener(OnRetryClicked);
            if (menuPauseButton != null) menuPauseButton.onClick.AddListener(OnMenuClicked);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
            MergeInvoker.OnMergeUndone -= HandleMergeUndone;
            MergeInvoker.OnMergeExecuted -= HandleMergeExecuted;
            GameManager.OnMoveCountChanged -= HandleMoveCountChanged;

        }

        #region Button Handlers

        private void OnPauseClicked()
        {
            if(AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX("ui button");
            }
            ShowPausePanel();
        }

        private void OnResumeClicked()
        {
            AudioManager.Instance?.PlaySFX("ui button");
            HidePausePanel();
        }

        private void OnNextLevelClicked()
        {
            Time.timeScale = 1f; // Ensure time scale is normal
            // Get current level index from PlayerPrefs
            int currentIndex = PlayerPrefs.GetInt("SelectedLevel", 0);
            int nextIndex = currentIndex + 1;

            // Check if there's a next level
            if (nextIndex >= LevelLoader.TotalLevelCount)
            {
                Debug.Log("[UIManager] No more levels! Returning to menu.");
                SceneManager.LoadScene("Menu");
                return;
            }

            // Set next level data
            LevelData nextLevel = LevelLoader.SelectLevel(nextIndex);
            if (nextLevel != null)
            {
                Debug.Log($"[UIManager] Loading next level: {nextLevel.LevelName}");
                SceneManager.LoadScene("MVPGameplay");
            }
            else
            {
                Debug.LogWarning("[UIManager] Failed to set next level, returning to menu.");
                SceneManager.LoadScene("Menu");
            }
        }

        private void OnRetryClicked()
        {
            AudioManager.Instance?.PlaySFX("ui button");
            Debug.Log("[UIManager] Retrying level");
            Time.timeScale = 1f; // Reset time scale
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void OnMenuClicked()
        {
            AudioManager.Instance?.PlaySFX("ui button");
            Debug.Log("[UIManager] Returning to menu");
            Time.timeScale = 1f; // Reset time scale
            SceneManager.LoadScene("Menu");
        }

        private void OnUndoClicked()
        {

            // Play undo SFX
            if(AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX("undo");
            }

            MergeInvoker.UndoMerge();
            Debug.Log("[UIManager] Undo executed successfully");
        }

        #endregion

        #region Pause Panel

        private void ShowPausePanel()
        {
            if (pausePanel == null) return;

            pausePanel.SetActive(true);
            Time.timeScale = 0f;

            // Animate
            if (pausePanel.GetComponent<CanvasGroup>() == null)
                pausePanel.AddComponent<CanvasGroup>();
            
            CanvasGroup cg = pausePanel.GetComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.DOFade(1f, 0.3f).SetUpdate(true); // SetUpdate(true) ignores timeScale

            RectTransform rt = pausePanel.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.localScale = Vector3.one * 0.9f;
                rt.DOScale(1f, 0.3f).SetEase(Ease.OutBack).SetUpdate(true);
            }
        }

        private void HidePausePanel()
        {
            if (pausePanel == null) return;

            Time.timeScale = 1f;

            // Animate
            CanvasGroup cg = pausePanel.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.DOFade(0f, 0.2f).SetUpdate(true).OnComplete(() =>
                {
                    pausePanel.SetActive(false);
                });
            }
            else
            {
                pausePanel.SetActive(false);
            }
        }

        #endregion

        #region Gameplay Score

        /// <summary>
        /// Update the gameplay score display with animation.
        /// </summary>
        public void UpdateMoves(int moves)
        {
            if (moveCountText == null) return;

            // Simple punch animation
            moveCountText.text = $"Moves: {moves}";
            
            // Kill existing tween on the transform to avoid conflicts
            moveCountText.transform.DOKill(true);
            
            // Punch scale effect
            moveCountText.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 10, 1f);
        }

        #endregion

       
        #region Win Panel

        /// <summary>
        /// Show win panel with star animation.
        /// </summary>
        public void ShowWinPanel(int stars, int score)
        {
            if (winPanel == null)
            {
                Debug.LogWarning("[UIManager] Win panel not assigned!");
                return;
            }

            // Play win SFX
            AudioManager.Instance.PlaySFX("win");
            AudioManager.Instance.PlaySFX("win2");

            // Update score text
            if (winScoreText != null)
            {
                winScoreText.text = $"Score: {score}";
            }

            // Update star display
            UpdateWinStars(stars);

            // Show panel with animation
            winPanel.SetActive(true);
            var panelRect = winPanel.GetComponent<RectTransform>();
            if (panelRect != null)
            {
                panelRect.localScale = Vector3.zero;
                panelRect.DOScale(1f, 0.4f).SetEase(Ease.OutBack);
            }

            // Animate stars sequentially
            AnimateStars(stars);
        }

        private void UpdateWinStars(int stars)
        {
            if (winStarImages == null) return;

            for (int i = 0; i < winStarImages.Length; i++)
            {
                if (winStarImages[i] != null)
                {
                    winStarImages[i].sprite = (i < stars) ? starFilledSprite : starEmptySprite;
                    winStarImages[i].transform.localScale = Vector3.zero;
                }
            }
        }

        private void AnimateStars(int stars)
        {
            if (winStarImages == null) return;

            for (int i = 0; i < winStarImages.Length && i < stars; i++)
            {
                if (winStarImages[i] != null)
                {
                    float delay = 0.5f + (i * 0.2f);
                    winStarImages[i].transform
                        .DOScale(1f, 0.3f)
                        .SetEase(Ease.OutBack)
                        .SetDelay(delay);
                }
            }

            // Show empty stars immediately (no animation)
            for (int i = stars; i < winStarImages.Length; i++)
            {
                if (winStarImages[i] != null)
                {
                    winStarImages[i].transform.localScale = Vector3.one;
                }
            }
        }

        /// <summary>
        /// Hide win panel.
        /// </summary>
        public void HideWinPanel()
        {
            if (winPanel != null)
            {
                winPanel.SetActive(false);
            }
        }

        #endregion
    }
}