using System.Collections;
using System.Collections.Generic;
using Blobs.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Blobs.Tests.PlayMode
{
    public sealed class WorldCarouselTests
    {
        [UnityTest]
        public IEnumerator SelectNext_SnapsToNextWorldAndRaisesSelection()
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

            Assert.That(carousel.SelectedIndex, Is.EqualTo(1));
            Assert.That(selectedIndex, Is.EqualTo(1));

            for (int i = 0; i < 30; i++)
                yield return null;

            Assert.That(content.anchoredPosition.x, Is.EqualTo(-430f).Within(1f));
            Object.Destroy(viewport);
        }

        private static RectTransform CreateItem(RectTransform parent, string name)
        {
            var item = new GameObject(name, typeof(RectTransform));
            item.transform.SetParent(parent, false);
            return item.GetComponent<RectTransform>();
        }
    }
}
