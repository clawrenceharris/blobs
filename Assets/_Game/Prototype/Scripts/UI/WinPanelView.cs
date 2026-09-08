using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Blobs.Core.UI
{

    public class WinPanelView : PanelView
    {
        [SerializeField] private Image[] _winStarImages;
        [SerializeField] private Sprite _starFilledSprite;
        [SerializeField] private Sprite _starEmptySprite;
        [SerializeField] private TextMeshProUGUI _winScoreText;
        [SerializeField] private Button _nextLevelButton;
        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _menuButton;

        public Button NextLevelButton => _nextLevelButton;
        public Button RetryButton => _retryButton;
        public Button MenuButton => _menuButton;

      

        /// <summary>
        /// Show win panel with star animation.
        /// </summary>
        public void ShowWinPanel(int stars, int score)
        {
           
            // Play win SFX
            AudioManager.Instance.PlaySFX("win");
            AudioManager.Instance.PlaySFX("win2");

            // Update score text
            if (_winScoreText != null)
            {
                _winScoreText.text = $"Score: {score}";
            }

            // Update star display
            UpdateWinStars(stars);

            // Show panel with animation
            _canvasGroup.gameObject.SetActive(true);
            
            _rectTransform.localScale = Vector3.zero;
            _rectTransform.DOScale(1f, 0.4f).SetEase(Ease.OutBack);
        
            // Animate stars sequentially
            AnimateStars(stars);
        }

        private void UpdateWinStars(int stars)
        {
            if (_winStarImages == null) return;

            for (int i = 0; i < _winStarImages.Length; i++)
            {
                if (_winStarImages[i] != null)
                {
                    _winStarImages[i].sprite = (i < stars) ? _starFilledSprite : _starEmptySprite;
                    _winStarImages[i].transform.localScale = Vector3.zero;
                }
            }
        }

        private void AnimateStars(int stars)
        {
            if (_winStarImages == null) return;

            for (int i = 0; i < _winStarImages.Length && i < stars; i++)
            {
                if (_winStarImages[i] != null)
                {
                    float delay = 0.5f + (i * 0.2f);
                    _winStarImages[i].transform
                        .DOScale(1f, 0.3f)
                        .SetEase(Ease.OutBack)
                        .SetDelay(delay);
                }
            }

            // Show empty stars immediately (no animation)
            for (int i = stars; i < _winStarImages.Length; i++)
            {
                if (_winStarImages[i] != null)
                {
                    _winStarImages[i].transform.localScale = Vector3.one;
                }
            }
        }

    }
}