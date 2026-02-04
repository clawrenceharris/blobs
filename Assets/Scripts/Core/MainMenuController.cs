using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;
using TMPro;
using Blobs.Services;

namespace Blobs.Core
{
    /// <summary>
    /// Main Menu Controller with DOTween animations.
    /// Handles menu navigation, level selection, and settings panel.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        #region UI References

        [Header("Main Menu Buttons")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;

        [Header("Panels")]
        [SerializeField] private RectTransform levelSelectionPanel;
        [SerializeField] private RectTransform settingsPanel;
        [SerializeField] private Button levelSelectionBackButton;
        [SerializeField] private Button settingsBackButton;

        [Header("Level Selection")]
        [SerializeField] private Button[] levelButtons = new Button[9];
        [SerializeField] private Image[] levelStarContainers = new Image[9]; // Parent for stars per level
        [SerializeField] private Sprite starFilledSprite;
        [SerializeField] private Sprite starEmptySprite;
        [SerializeField] private Color lockedLevelColor = new Color(0.5f, 0.5f, 0.5f, 0.7f);

        [Header("Settings")]
        [SerializeField] private Slider bgmVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;

        [Header("Animation Settings")]
        [SerializeField] private float panelSlideDuration = 0.3f;
        [SerializeField] private float buttonHoverScale = 1.1f;
        [SerializeField] private float buttonHoverDuration = 0.15f;
        [SerializeField] private Ease panelEase = Ease.OutBack;

        [SerializeField] private RectTransform[] ornamentalBlobs;
        [SerializeField] private RectTransform titleImage;

        [Header("Scene Names")]
        [SerializeField] private string gameplaySceneName = "MVPGameplay";

        [Header("Level Data")]
        [SerializeField] private LevelData[] allLevels;
        
        /// <summary>
        /// Static reference to pass selected level data to the gameplay scene.
        /// </summary>
        public static LevelData SelectedLevelData { get; private set; }

        /// <summary>
        /// Static reference to all levels for UI access.
        /// </summary>
        public static LevelData[] AllLevels { get; private set; }

        /// <summary>
        /// Total number of available levels.
        /// </summary>
        public static int TotalLevelCount => AllLevels?.Length ?? 0;

        /// <summary>
        /// Set the selected level by index and return the level data.
        /// </summary>
        public static LevelData SetSelectedLevel(int levelIndex)
        {
            if (AllLevels == null || levelIndex < 0 || levelIndex >= AllLevels.Length)
            {
                Debug.LogWarning($"[MainMenuController] Invalid level index: {levelIndex}");
                return null;
            }

            SelectedLevelData = AllLevels[levelIndex];
            PlayerPrefs.SetInt("SelectedLevel", levelIndex);
            PlayerPrefs.Save();
            return SelectedLevelData;
        }

        #endregion

        #region Private Fields

        private Vector2 levelPanelHiddenPos;
        private Vector2 levelPanelShownPos;
        private Vector2 settingsPanelHiddenPos;
        private Vector2 settingsPanelShownPos;
        private bool isAnimating = false;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Set static reference for other scripts to access
            AllLevels = allLevels;

            SetupPanelPositions();
            SetupButtonListeners();
            SetupVolumeSliders();
            SetupButtonHoverEffects();
        }

        private void Start()
        {
            // Initialize level selection UI
            RefreshLevelSelectionUI();

            // Play menu BGM if available
            if (AudioManager.Instance != null && AudioManager.Instance.HasClip("menu"))
            {
                AudioManager.Instance.PlayBGM("menu");
            }

            SetupIdleAnimations();
        }

        private void SetupIdleAnimations()
        {
            // Title Animation (Scale Pulse)
            if (titleImage != null)
            {
                titleImage.DOScale(1.05f, 2f)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine);
            }

            // Ornamental Blobs Animations (Varied Floating/Scaling)
            if (ornamentalBlobs != null)
            {
                for (int i = 0; i < ornamentalBlobs.Length; i++)
                {
                    var blob = ornamentalBlobs[i];
                    if (blob == null) continue;

                    // Randomize parameters for natural feel
                    float duration = Random.Range(1.5f, 2.5f);
                    float delay = Random.Range(0f, 1f);
                    float moveAmount = Random.Range(15f, 25f);
                    float scaleAmount = Random.Range(1.05f, 1.15f);

                    // Randomly choose between just floating or floating + scaling
                    // Use a unique ID for each tween to avoid conflicts if we need to kill them later
                    
                    // Floating Y (Up/Down)
                    blob.DOAnchorPosY(blob.anchoredPosition.y + moveAmount, duration)
                        .SetLoops(-1, LoopType.Yoyo)
                        .SetEase(Ease.InOutSine)
                        .SetDelay(delay);

                    // Subtle Scaling
                    blob.DOScale(scaleAmount, duration * 1.2f)
                        .SetLoops(-1, LoopType.Yoyo)
                        .SetEase(Ease.InOutQuad)
                        .SetDelay(delay);
                }
            }
        }

        #endregion

        #region Setup

        private void SetupPanelPositions()
        {
            // Level Selection Panel - slide from right
            if (levelSelectionPanel != null)
            {
                levelPanelShownPos = Vector2.zero;
                levelPanelHiddenPos = new Vector2(Screen.width, 0);
                levelSelectionPanel.anchoredPosition = levelPanelHiddenPos;
                levelSelectionPanel.gameObject.SetActive(false);
            }

            // Settings Panel - slide from right
            if (settingsPanel != null)
            {
                settingsPanelShownPos = Vector2.zero;
                settingsPanelHiddenPos = new Vector2(Screen.width, 0);
                settingsPanel.anchoredPosition = settingsPanelHiddenPos;
                settingsPanel.gameObject.SetActive(false);
            }
        }

        private void SetupButtonListeners()
        {
            // Main menu buttons
            if (playButton != null)
                playButton.onClick.AddListener(OnPlayClicked);

            if (settingsButton != null)
                settingsButton.onClick.AddListener(OnSettingsClicked);

            if (quitButton != null)
                quitButton.onClick.AddListener(OnQuitClicked);

            // Back buttons
            if (levelSelectionBackButton != null)
                levelSelectionBackButton.onClick.AddListener(OnLevelSelectionBackClicked);

            if (settingsBackButton != null)
                settingsBackButton.onClick.AddListener(OnSettingsBackClicked);

            // Level buttons
            for (int i = 0; i < levelButtons.Length; i++)
            {
                if (levelButtons[i] != null)
                {
                    int levelIndex = i; // Capture for closure
                    levelButtons[i].onClick.AddListener(() => OnLevelClicked(levelIndex));
                }
            }
        }

        private void SetupVolumeSliders()
        {
            if (bgmVolumeSlider != null)
            {
                bgmVolumeSlider.value = AudioManager.Instance?.GetBGMVolume() ?? 1f;
                bgmVolumeSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.value = AudioManager.Instance?.GetSFXVolume() ?? 1f;
                sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
            }
        }

        private void SetupButtonHoverEffects()
        {
            // Add hover effects to all main buttons
            AddHoverEffect(playButton);
            AddHoverEffect(settingsButton);
            AddHoverEffect(quitButton);
            AddHoverEffect(levelSelectionBackButton);
            AddHoverEffect(settingsBackButton);

            // Add hover effects to level buttons
            foreach (var btn in levelButtons)
            {
                AddHoverEffect(btn);
            }
        }

        private void AddHoverEffect(Button button)
        {
            if (button == null) return;

            var trigger = button.gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();
            if (trigger == null)
                trigger = button.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();

            // Pointer Enter
            var enterEntry = new UnityEngine.EventSystems.EventTrigger.Entry
            {
                eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter
            };
            enterEntry.callback.AddListener((data) =>
            {
                button.transform.DOScale(buttonHoverScale, buttonHoverDuration).SetEase(Ease.OutQuad);
            });
            trigger.triggers.Add(enterEntry);

            // Pointer Exit
            var exitEntry = new UnityEngine.EventSystems.EventTrigger.Entry
            {
                eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit
            };
            exitEntry.callback.AddListener((data) =>
            {
                button.transform.DOScale(1f, buttonHoverDuration).SetEase(Ease.OutQuad);
            });
            trigger.triggers.Add(exitEntry);
        }

        #endregion

        #region Main Menu Button Handlers

        private void OnPlayClicked()
        {
            if (isAnimating) return;
            PlayButtonSFX();
            ShowPanel(levelSelectionPanel, levelPanelHiddenPos, levelPanelShownPos);
        }

        private void OnSettingsClicked()
        {
            if (isAnimating) return;
            PlayButtonSFX();
            ShowPanel(settingsPanel, settingsPanelHiddenPos, settingsPanelShownPos);
        }

        private void OnQuitClicked()
        {
            PlayButtonSFX();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        #endregion

        #region Level Selection

        private void OnLevelClicked(int levelIndex)
        {
            if (isAnimating) return;

            // Check if level is unlocked
            if (!LevelProgressManager.IsLevelUnlocked(levelIndex))
            {
                Debug.Log($"[MainMenuController] Level {levelIndex + 1} is locked.");
                return;
            }

            PlayButtonSFX();

            // Store selected level index for persistence
            PlayerPrefs.SetInt("SelectedLevel", levelIndex);
            PlayerPrefs.Save();

            // Set static data for GamePresenter to pick up
            if (allLevels != null && levelIndex < allLevels.Length)
            {
                SelectedLevelData = allLevels[levelIndex];
                Debug.Log($"[MainMenuController] Selected Level Data: {SelectedLevelData.name}");
            }
            else
            {
                Debug.LogWarning("[MainMenuController] Level data missing for index " + levelIndex);
            }

            // Load gameplay scene
            SceneManager.LoadScene(gameplaySceneName);
        }

        private void OnLevelSelectionBackClicked()
        {
            if (isAnimating) return;
            PlayButtonSFX();
            HidePanel(levelSelectionPanel, levelPanelHiddenPos);
        }

        /// <summary>
        /// Refresh the level selection UI to show current progress.
        /// </summary>
        public void RefreshLevelSelectionUI()
        {
            for (int i = 0; i < levelButtons.Length; i++)
            {
                if (levelButtons[i] == null) continue;

                bool isUnlocked = LevelProgressManager.IsLevelUnlocked(i);
                int stars = LevelProgressManager.GetStars(i);

                // Set button interactability
                levelButtons[i].interactable = isUnlocked;

                // Update visual state
                var buttonImage = levelButtons[i].GetComponent<Image>();
                if (buttonImage != null)
                {
                    buttonImage.color = isUnlocked ? Color.white : lockedLevelColor;
                }

                // Update star display
                UpdateStarDisplay(i, stars, isUnlocked);
            }
        }

        private void UpdateStarDisplay(int levelIndex, int stars, bool isUnlocked)
        {
            if (levelIndex >= levelStarContainers.Length || levelStarContainers[levelIndex] == null)
                return;

            // Get all images (parent IS star 1, children are star 2 and 3)
            // GetComponentsInChildren returns parent first, then children in hierarchy order
            var starImages = levelStarContainers[levelIndex].GetComponentsInChildren<Image>(true);
            
            // Update up to 3 stars
            for (int s = 0; s < 3; s++)
            {
                if (s >= starImages.Length) break;

                var starImg = starImages[s];
                if (!isUnlocked)
                {
                    starImg.gameObject.SetActive(false);
                }
                else
                {
                    starImg.gameObject.SetActive(true);
                    starImg.sprite = (s < stars) ? starFilledSprite : starEmptySprite;
                }
            }
        }

        #endregion

        #region Settings

        private void OnSettingsBackClicked()
        {
            if (isAnimating) return;
            PlayButtonSFX();
            HidePanel(settingsPanel, settingsPanelHiddenPos);
        }

        private void OnBGMVolumeChanged(float value)
        {
            AudioManager.Instance?.SetBGMVolume(value);
        }

        private void OnSFXVolumeChanged(float value)
        {
            AudioManager.Instance?.SetSFXVolume(value);
        }

        #endregion

        #region Panel Animations

        private void ShowPanel(RectTransform panel, Vector2 fromPos, Vector2 toPos)
        {
            if (panel == null) return;

            isAnimating = true;
            panel.gameObject.SetActive(true);
            panel.anchoredPosition = fromPos;

            panel.DOAnchorPos(toPos, panelSlideDuration)
                .SetEase(panelEase)
                .OnComplete(() => isAnimating = false);
        }

        private void HidePanel(RectTransform panel, Vector2 toPos)
        {
            if (panel == null) return;

            isAnimating = true;

            panel.DOAnchorPos(toPos, panelSlideDuration * 0.8f)
                .SetEase(Ease.InBack)
                .OnComplete(() =>
                {
                    panel.gameObject.SetActive(false);
                    isAnimating = false;
                });
        }

        #endregion

        #region Audio

        private void PlayButtonSFX()
        {
            if (AudioManager.Instance != null && AudioManager.Instance.HasClip("ui button"))
            {
                AudioManager.Instance.PlaySFX("ui button");
            }
        }

        #endregion
    }
}
