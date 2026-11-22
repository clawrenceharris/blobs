using System.Collections;
using System.Linq;
using Blobs.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Blobs.Tests.PlayMode
{
    public sealed class WorldsUiThemeLayoutTests
    {
        private const float ExpectedButtonWidth = 680.9467f;
        private const float ExpectedButtonHeight = 401.70203f;

        [UnitySetUp]
        public IEnumerator LoadWorlds()
        {
            SceneManager.LoadScene("Worlds", LoadSceneMode.Single);
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator WorldsLayout_UsesThemeAndRequiredReferenceProportions()
        {
            WorldsResponsiveLayout layout = Object.FindFirstObjectByType<WorldsResponsiveLayout>();
            Assert.That(layout, Is.Not.Null);
            Assert.That(layout.Theme, Is.Not.Null);

            RectTransform safeArea = layout.GetComponent<RectTransform>();
            RectTransform progress = FindRect("ProgressBar");
            RectTransform button = FindRect("EnterButton");
            RectTransform card = FindRect("WorldCardTemplate");
            WorldCarousel carousel = Object.FindFirstObjectByType<WorldCarousel>();

            layout.RefreshLayout();
            yield return null;

            Assert.That(
                card.rect.width * 0.76f / safeArea.rect.width,
                Is.EqualTo(WorldsResponsiveLayout.HeroWidthRatio).Within(0.01f));
            Assert.That(
                progress.rect.width / safeArea.rect.width,
                Is.EqualTo(WorldsResponsiveLayout.ProgressWidthRatio).Within(0.01f));
            Assert.That(carousel.SideScale, Is.EqualTo(0.4f).Within(0.001f));
            Assert.That(carousel.ItemSpacing, Is.EqualTo(380f).Within(0.001f));

            float clusterGap = progress.rect.yMin + progress.anchoredPosition.y
                - (button.rect.yMax + button.anchoredPosition.y);
            Assert.That(clusterGap, Is.EqualTo(layout.Theme.Cluster).Within(0.5f));

            Assert.That(button.sizeDelta.x, Is.EqualTo(ExpectedButtonWidth).Within(0.01f));
            Assert.That(button.sizeDelta.y, Is.EqualTo(ExpectedButtonHeight).Within(0.01f));
        }

        [UnityTest]
        public IEnumerator WorldsLayout_ReflowsAcrossPortraitSafeAreasWithoutChangingButtonSize()
        {
            WorldsResponsiveLayout layout = Object.FindFirstObjectByType<WorldsResponsiveLayout>();
            RectTransform safeArea = layout.GetComponent<RectTransform>();
            SafeAreaLayout safeAreaDriver = safeArea.GetComponent<SafeAreaLayout>();
            RectTransform progress = FindRect("ProgressBar");
            RectTransform card = FindRect("WorldCardTemplate");
            RectTransform button = FindRect("EnterButton");
            Vector2 buttonSize = button.sizeDelta;

            safeAreaDriver.enabled = false;
            safeArea.anchorMin = new Vector2(0.5f, 0.5f);
            safeArea.anchorMax = new Vector2(0.5f, 0.5f);

            foreach (Vector2 size in new[] { new Vector2(900f, 1600f), new Vector2(1200f, 2400f) })
            {
                safeArea.sizeDelta = size;
                layout.RefreshLayout();
                Canvas.ForceUpdateCanvases();
                yield return null;

                Assert.That(
                    card.rect.width * 0.76f / safeArea.rect.width,
                    Is.EqualTo(WorldsResponsiveLayout.HeroWidthRatio).Within(0.01f));
                Assert.That(
                    progress.rect.width / safeArea.rect.width,
                    Is.EqualTo(WorldsResponsiveLayout.ProgressWidthRatio).Within(0.01f));
                Assert.That(button.sizeDelta, Is.EqualTo(buttonSize));
            }
        }

        [UnityTest]
        public IEnumerator LockedCarouselCard_UsesOpaqueThemeTintAndNoArrowObjectsExist()
        {
            yield return null;
            UiThemeAsset theme = Object.FindFirstObjectByType<WorldsResponsiveLayout>().Theme;
            WorldCarouselCardView[] cards = Object.FindObjectsByType<WorldCarouselCardView>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            WorldCarouselCardView lockedCard = cards.FirstOrDefault(card =>
                card.gameObject.activeInHierarchy &&
                card.GetComponentsInChildren<UnityEngine.UI.Image>(true)
                    .Any(image => image.name == "LockedIcon" && image.gameObject.activeSelf));

            Assert.That(lockedCard, Is.Not.Null);
            Assert.That(lockedCard.Theme, Is.SameAs(theme));
            UnityEngine.UI.Image lockedVisual = lockedCard
                .GetComponentsInChildren<UnityEngine.UI.Image>(true)
                .First(image => image.name != "LockedIcon" &&
                    image.name.IndexOf("shadow", System.StringComparison.OrdinalIgnoreCase) < 0);
            Assert.That(lockedVisual.color, Is.EqualTo(theme.LockedContent));
            Assert.That(lockedVisual.color.a, Is.EqualTo(1f).Within(0.001f));

            Transform[] allTransforms = Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            Assert.That(allTransforms.Any(transform =>
                transform.name == "LeftArrow" || transform.name == "RightArrow"), Is.False);
        }

        private static RectTransform FindRect(string objectName)
        {
            return Object.FindObjectsByType<RectTransform>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .First(rect => rect.name == objectName);
        }
    }
}
