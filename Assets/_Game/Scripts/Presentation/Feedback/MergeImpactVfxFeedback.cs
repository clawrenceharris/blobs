using UnityEngine;
using UnityEngine.Rendering;

namespace Blobs.Presentation
{
    /// <summary>
    /// Instantiates and configures the authored visual-effects channel for a merge impact.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MergeImpactVfxFeedback : MonoBehaviour, IMergeImpactFeedback
    {
        [Tooltip("Scene-authored effect prefab containing particle, flash, and ring layers.")]
        [SerializeField] private MergeImpactVfx _prefab;
        [SerializeField] private Transform _effectsRoot;
        [SerializeField, Min(1)] private int _sortingOffset = 10;

        private bool _reportedMissingPrefab;

        /// <inheritdoc />
        public void PlayImpact(MergeImpactFeedbackContext context)
        {
            if (_prefab == null)
            {
                ReportMissingPrefab();
                return;
            }

            ResolveSorting(
                context.SortingAnchor,
                out int sortingLayerId,
                out int sortingOrder);
            Transform parent = _effectsRoot != null ? _effectsRoot : transform;
            MergeImpactVfx effect = Instantiate(
                _prefab,
                context.WorldPosition,
                Quaternion.identity,
                parent);
            effect.Play(context.BlobColor, sortingLayerId, sortingOrder);
        }

        private void ResolveSorting(
            BlobView anchor,
            out int sortingLayerId,
            out int sortingOrder)
        {
            SortingGroup sortingGroup = anchor != null ? anchor.SortingGroup : null;
            sortingLayerId = sortingGroup != null ? sortingGroup.sortingLayerID : 0;
            sortingOrder = sortingGroup != null
                ? sortingGroup.sortingOrder + _sortingOffset
                : HighestSortingOrder(anchor) + _sortingOffset;
        }

        private static int HighestSortingOrder(BlobView view)
        {
            if (view == null)
                return 0;

            int order = 0;
            foreach (SpriteRenderer renderer in view.GetComponentsInChildren<SpriteRenderer>(true))
                order = Mathf.Max(order, renderer.sortingOrder);
            return order;
        }

        private void ReportMissingPrefab()
        {
            if (_reportedMissingPrefab)
                return;

            _reportedMissingPrefab = true;
            Debug.LogWarning(
                "Merge impact VFX has no authored MergeImpactVfx prefab assigned.",
                this);
        }
    }
}
