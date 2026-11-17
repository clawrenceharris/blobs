using System.Collections.Generic;
using Blobs.Content;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Blobs.UI
{
    /// <summary>
    /// Combined world carousel and level-page skeleton.
    /// </summary>
    public sealed class WorldsScreenPresenter : MonoBehaviour
    {
        [SerializeField] private Button backButton;
        [SerializeField] private List<WorldDefinitionAsset> worlds = new();

        [Header("World carousel")]
        [SerializeField] private WorldCarousel worldCarousel;
        [SerializeField] private RectTransform worldCarouselContent;
        [SerializeField] private WorldCarouselCardView worldCardTemplate;
        [SerializeField] private TMP_Text pageLabel;
        [SerializeField] private TMP_Text worldTitle;

        [SerializeField] private PlayButtonView enterButton;

        private void Awake()
        {
            if (backButton != null)
                backButton.onClick.AddListener(OnBack);
            if (enterButton != null)
            {
                enterButton.Button.onClick.AddListener(PlayerFlow.GoLevels);

            }

            BuildWorldCards();

        }


        private void OnDestroy()
        {
            if (backButton != null)
                backButton.onClick.RemoveListener(OnBack);
        }

        private void OnBack() => PlayerFlow.GoHome();

        private void BuildWorldCards()
        {
            if (worldCarousel == null || worldCarouselContent == null || worldCardTemplate == null)
                return;

            worldCardTemplate.gameObject.SetActive(false);
            var cards = new List<RectTransform>(worlds.Count);
            for (int i = 0; i < worlds.Count; i++)
            {
                WorldDefinitionAsset world = worlds[i];
                WorldCarouselCardView card = Instantiate(worldCardTemplate, worldCarouselContent);
                card.gameObject.SetActive(true);
                card.name = $"WorldCard_{i + 1:00}";
                card.SetContent(
                i + 1,
                world.Unlocked ? world.MechanicPreviewPrefab : world.MechanicPreviewLockedPrefab,
                world.Unlocked);
                cards.Add(card.RectTransform);
            }

            worldCarousel.Configure(worldCarouselContent, cards, 0, ShowWorld);
        }

        private void ShowWorld(int worldIndex)
        {
            if (worldIndex < 0 || worldIndex >= worlds.Count)
                return;

            WorldDefinitionAsset world = worlds[worldIndex];
            if (pageLabel != null)
                pageLabel.text = "WORLD " + (worldIndex + 1);
            if (worldTitle != null)
                worldTitle.text = world.Unlocked ? world.WorldTitle : "???";
            if (enterButton != null)
            {
                enterButton.SetEnabled(world.Unlocked);
                enterButton.SetLabel(world.Unlocked ? "Enter World" : "Locked");
            }

        }
    }
}
