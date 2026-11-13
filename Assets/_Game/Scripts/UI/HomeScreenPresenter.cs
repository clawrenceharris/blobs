using UnityEngine;
using UnityEngine.UI;

namespace Blobs.UI
{
    /// <summary>
    /// Home screen skeleton: Play opens Levels; Settings opens the side drawer.
    /// </summary>
    public sealed class HomeScreenPresenter : MonoBehaviour
    {
        [SerializeField] private Button playButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private SettingsDrawerView settingsDrawer;

        private void Awake()
        {
            if (playButton != null)
                playButton.onClick.AddListener(OnPlay);
            if (settingsButton != null)
                settingsButton.onClick.AddListener(OnSettings);
        }

        private void OnDestroy()
        {
            if (playButton != null)
                playButton.onClick.RemoveListener(OnPlay);
            if (settingsButton != null)
                settingsButton.onClick.RemoveListener(OnSettings);
        }

        private void OnPlay() => PlayerFlow.GoLevels();

        private void OnSettings()
        {
            if (settingsDrawer != null)
                settingsDrawer.Open();
        }
    }
}
