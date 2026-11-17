using UnityEngine;
using UnityEngine.EventSystems;

namespace Blobs.UI
{
    /// <summary>
    /// Turns a horizontal swipe over the level panel into one discrete world page change.
    /// </summary>
    public sealed class WorldSwipePager : MonoBehaviour, IBeginDragHandler, IEndDragHandler
    {
        [SerializeField] private WorldCarousel carousel;
        [SerializeField, Min(1f)] private float swipeThreshold = 80f;

        private Vector2 _dragStart;

        public void Configure(WorldCarousel worldCarousel)
        {
            carousel = worldCarousel;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _dragStart = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (carousel == null)
                return;

            float distance = eventData.position.x - _dragStart.x;
            if (Mathf.Abs(distance) < swipeThreshold)
                return;

            if (distance < 0f)
                carousel.SelectNext();
            else
                carousel.SelectPrevious();
        }
    }
}
