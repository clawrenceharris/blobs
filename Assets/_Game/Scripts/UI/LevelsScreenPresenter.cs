using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Blobs.UI
{
    /// <summary>
    /// Levels screen skeleton. Placeholder list until a real level map exists.
    /// </summary>
    public sealed class LevelsScreenPresenter : MonoBehaviour
    {
        [SerializeField] private Button backButton;
        [SerializeField, Min(1)] private int levelCount = 7;
        [SerializeField] private Transform levelListRoot;
        [SerializeField] private Button levelButtonTemplate;

        private void Awake()
        {
            if (backButton != null)
                backButton.onClick.AddListener(OnBack);

            BuildLevelButtons();
        }

        private void OnDestroy()
        {
            if (backButton != null)
                backButton.onClick.RemoveListener(OnBack);
        }

        private void OnBack() => PlayerFlow.GoHome();

        private void BuildLevelButtons()
        {
            if (levelListRoot == null || levelButtonTemplate == null)
                return;

            levelButtonTemplate.gameObject.SetActive(false);

            for (int i = 0; i < levelCount; i++)
            {
                int levelIndex = i;
                Button button = Instantiate(levelButtonTemplate, levelListRoot);
                button.gameObject.SetActive(true);
                button.name = $"LevelButton_{levelIndex + 1}";

                TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
                if (label != null)
                    label.text = $"Level {levelIndex + 1}";

                button.onClick.AddListener(() => PlayerFlow.GoGame(levelIndex));
            }
        }
    }
}
