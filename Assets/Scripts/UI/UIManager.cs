using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using UnityEngine.SceneManagement;
using Blobs.Core.Merge;
using System.Collections;

namespace Blobs.Core.UI
{

    public class UIManager : MonoBehaviour
    {

        [SerializeField] private PausePanelView _pauseView;

        [SerializeField] private WinPanelView _winView;

        [SerializeField] private TutorialView _tutorialView;

        [SerializeField] private HUDView _hudView;

        public PausePanelView PauseView => _pauseView;
        public WinPanelView WinView => _winView;
        public HUDView HudView => _hudView;
        public TutorialView TutorialView => _tutorialView;


        private void Start()
        {
            SetupButtonListeners();

        }


        private void SetupButtonListeners()
        {
            _pauseView.RetryButton.onClick.AddListener(OnRetryClicked);
            _pauseView.ResumeButton.onClick.AddListener(OnResumeClicked);
            _pauseView.MenuButton.onClick.AddListener(OnMenuClicked);
            _winView.NextLevelButton.onClick.AddListener(OnNextLevelClicked);
            _winView.RetryButton.onClick.AddListener(OnRetryClicked);
            _winView.MenuButton.onClick.AddListener(OnMenuClicked);
            _hudView.UndoButton.onClick.AddListener(OnUndoClicked);
            _hudView.PauseButton.onClick.AddListener(OnPauseClicked);
        }
        private void OnResumeClicked()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX("ui button");
            }
            _pauseView.HidePanel();
        }



        #region Button Handlers


        private void OnUndoClicked()
        {

            // Play undo SFX
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX("undo");
            }

            MergeInvoker.UndoMerge();
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
                PlayerPrefs.SetInt("SelectedLevel", nextIndex);
                PlayerPrefs.Save();
                Debug.Log($"[UIManager] Loading next level: {nextLevel.LevelName}");
                SceneManager.LoadScene("Blobs");
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
        private void OnPauseClicked()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX("ui button");
            }
            _pauseView.ShowPanel();
        }
        private void OnMenuClicked()
        {
            AudioManager.Instance?.PlaySFX("ui button");
            Debug.Log("[UIManager] Returning to menu");
            Time.timeScale = 1f; // Reset time scale
            SceneManager.LoadScene("Menu");
        }



        #endregion








    }
}