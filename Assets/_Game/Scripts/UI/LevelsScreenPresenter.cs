using System.Collections.Generic;
using Blobs.Application;
using Blobs.Content;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Blobs.UI
{
    /// <summary>
    /// Combined world carousel and level-page skeleton.
    /// </summary>
    public sealed class LevelsScreenPresenter : MonoBehaviour
    {
        [SerializeField] private List<WorldDefinitionAsset> worlds = new();

        [SerializeField] private Button backButton;

        [SerializeField] private TMP_Text worldTitle;
        [SerializeField] private TMP_Text worldNumber;

        [SerializeField] private TMP_Text worldDescription;
        [SerializeField] private TMP_Text pageLabel;
        [SerializeField] private Image lockImage;

        [Header("Level page")]
        [SerializeField] private Transform levelListRoot;
        [SerializeField] private UiButtonView levelButtonTemplate;
        [SerializeField] private WorldSwipePager levelSwipePager;

        private void Awake()
        {
            if (backButton != null)
                backButton.onClick.AddListener(OnBack);
            ShowWorld();
        }

        private void OnDestroy()
        {
            if (backButton != null)
                backButton.onClick.RemoveListener(OnBack);
        }

        private void OnBack() => PlayerFlow.GoWorlds();



        private void ShowWorld()
        {

            PlayerFlowState.TryConsumePendingWorldIndex(out int worldIndex);


            if (worldIndex < 0 || worldIndex >= worlds.Count)
                return;
            WorldDefinitionAsset world = worlds[worldIndex];
            if (worldTitle != null)
                worldTitle.text = world.WorldTitle;
            if (worldDescription != null)
                worldDescription.text = world.WorldDescription;
            if (worldNumber != null)
                worldNumber.text = $"WORLD {worldIndex + 1}";
            if (pageLabel != null)
                pageLabel.text = $"{worldIndex + 1} / {worlds.Count}";
            BuildLevelButtons(worldIndex, world);




        }

        private void BuildLevelButtons(int worldIndex, WorldDefinitionAsset world)
        {
            if (levelListRoot == null || levelButtonTemplate == null)
                return;

            levelButtonTemplate.gameObject.SetActive(false);
            for (int i = levelListRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = levelListRoot.GetChild(i);
                if (child != levelButtonTemplate.transform)
                    Destroy(child.gameObject);
            }

            int firstLevelIndex = 0;
            for (int i = 0; i < worldIndex; i++)
                firstLevelIndex += worlds[i].Levels.Count;

            for (int i = 0; i < world.LevelSlotCount; i++)
            {
                int levelIndex = i;
                UiButtonView buttonView = Instantiate(levelButtonTemplate, levelListRoot);
                buttonView.gameObject.SetActive(true);
                buttonView.name = $"LevelButton_{levelIndex + 1}";

                bool available = world.Unlocked && levelIndex < world.Levels.Count;
                buttonView.SetLabelText(available ? $"{levelIndex + 1}" : "");
                if (buttonView.ButtonIcon != null)
                {
                    buttonView.ButtonIcon.gameObject.SetActive(available);
                }


                buttonView.SetEnabled(available);
                if (available)
                {
                    int gameLevelIndex = firstLevelIndex + levelIndex;
                    buttonView.Button.onClick.AddListener(() => PlayerFlow.GoGame(gameLevelIndex));
                }
            }
        }

    }
}
