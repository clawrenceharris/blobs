using System.Collections.Generic;
using Blobs.UI;
using NUnit.Framework;
using UnityEngine;

namespace Blobs.Tests.EditMode
{
    public sealed class WorldCarouselTests
    {
        [Test]
        public void ConfigureAndSelectNext_UsesStableSpacingAndReportsFocusedWorld()
        {
            var viewport = new GameObject("Viewport", typeof(RectTransform));
            var contentObject = new GameObject("Content", typeof(RectTransform));
            contentObject.transform.SetParent(viewport.transform, false);
            var content = contentObject.GetComponent<RectTransform>();
            var first = CreateItem(content, "First");
            var second = CreateItem(content, "Second");
            var carousel = viewport.AddComponent<WorldCarousel>();
            int selectedIndex = -1;

            carousel.Configure(
                content,
                new List<RectTransform> { first, second },
                0,
                index => selectedIndex = index);
            carousel.SelectNext();

            Assert.That(first.anchoredPosition.x, Is.EqualTo(0f));
            Assert.That(second.anchoredPosition.x, Is.EqualTo(430f));
            Assert.That(carousel.SelectedIndex, Is.EqualTo(1));
            Assert.That(selectedIndex, Is.EqualTo(1));
            Object.DestroyImmediate(viewport);
        }

        private static RectTransform CreateItem(RectTransform parent, string name)
        {
            var item = new GameObject(name, typeof(RectTransform));
            item.transform.SetParent(parent, false);
            return item.GetComponent<RectTransform>();
        }
    }
}
