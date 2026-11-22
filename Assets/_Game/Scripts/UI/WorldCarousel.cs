using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Blobs.UI
{
    /// <summary>
    /// Horizontal carousel that always settles with one world centered.
    /// </summary>
    public sealed class WorldCarousel : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private RectTransform content;
        [SerializeField, Min(1f)] private float itemSpacing = 430f;
        [SerializeField, Min(0f)] private float snapSpeed = 12f;
        [SerializeField, Range(0.1f, 1f)] private float sideScale = 0.62f;

        private readonly List<RectTransform> _items = new();
        private Action<int> _selectionChanged;
        private Canvas _canvas;
        private int _selectedIndex;
        private bool _dragging;

        public int SelectedIndex => _selectedIndex;
        public float ItemSpacing => itemSpacing;
        public float SideScale => sideScale;

        public void Configure(
            RectTransform carouselContent,
            IReadOnlyList<RectTransform> items,
            int selectedIndex,
            Action<int> selectionChanged)
        {
            content = carouselContent;
            _items.Clear();
            for (int i = 0; i < items.Count; i++)
            {
                RectTransform item = items[i];
                _items.Add(item);
                item.anchorMin = new Vector2(0f, 0.5f);
                item.anchorMax = new Vector2(0f, 0.5f);
                item.pivot = new Vector2(0.5f, 0.5f);
                item.anchoredPosition = new Vector2(i * itemSpacing, 0f);
                item.localScale = Vector3.one;
            }

            _selectionChanged = selectionChanged;
            _canvas = GetComponentInParent<Canvas>();
            SetSelectedIndex(selectedIndex, true);
        }

        public void SelectPrevious() => SetSelectedIndex(_selectedIndex - 1, false);

        public void SelectNext() => SetSelectedIndex(_selectedIndex + 1, false);

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_items.Count > 1)
                _dragging = true;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_dragging || content == null)
                return;

            float scaleFactor = _canvas != null ? _canvas.scaleFactor : 1f;
            Vector2 position = content.anchoredPosition;
            position.x = Mathf.Clamp(
                position.x + eventData.delta.x / Mathf.Max(scaleFactor, 0.01f),
                -(_items.Count - 1) * itemSpacing,
                0f);
            content.anchoredPosition = position;
            UpdateSelectedFromPosition();
            UpdateItemScales();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _dragging = false;
            UpdateSelectedFromPosition();
        }

        private void Update()
        {
            if (_dragging || content == null || _items.Count == 0)
                return;

            Vector2 target = new(-_selectedIndex * itemSpacing, content.anchoredPosition.y);
            float t = 1f - Mathf.Exp(-snapSpeed * Time.unscaledDeltaTime);
            content.anchoredPosition = Vector2.Lerp(content.anchoredPosition, target, t);
            UpdateItemScales();
        }

        private void UpdateSelectedFromPosition()
        {
            int nearest = Mathf.RoundToInt(-content.anchoredPosition.x / itemSpacing);
            SetSelectedIndex(nearest, false);
        }

        private void SetSelectedIndex(int index, bool immediate)
        {
            if (_items.Count == 0 || content == null)
                return;

            int clamped = Mathf.Clamp(index, 0, _items.Count - 1);
            bool changed = clamped != _selectedIndex;
            _selectedIndex = clamped;

            if (immediate)
            {
                Vector2 position = content.anchoredPosition;
                position.x = -_selectedIndex * itemSpacing;
                content.anchoredPosition = position;
                UpdateItemScales();
            }

            if (changed || immediate)
                _selectionChanged?.Invoke(_selectedIndex);
        }

        private void UpdateItemScales()
        {
            float carouselPosition = -content.anchoredPosition.x / itemSpacing;
            for (int i = 0; i < _items.Count; i++)
            {
                float focus = 1f - Mathf.Clamp01(Mathf.Abs(i - carouselPosition));
                float scale = Mathf.Lerp(sideScale, 1f, focus);
                _items[i].localScale = new Vector3(scale, scale, 1f);
            }
        }
    }
}
