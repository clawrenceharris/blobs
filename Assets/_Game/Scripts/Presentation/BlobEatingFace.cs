using UnityEngine;

namespace Blobs.Presentation
{
    /// <summary>Per-monster identity is the idle sprite, never the monster's body color.</summary>
    public sealed class BlobEatingFace : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer faceRenderer;
        [SerializeField] private MonsterFacePairs facePairs;
        private Sprite _idle;
        private bool _captured;
        public void BeginPreview(MonsterFacePairs fallback)
        {
            if (facePairs == null) facePairs = fallback;
            if (faceRenderer == null)
                foreach (var renderer in GetComponentsInChildren<SpriteRenderer>(true))
                    if (renderer.name == "Face") { faceRenderer = renderer; break; }
            if (faceRenderer == null) return;
            _idle = faceRenderer.sprite;
            _captured = true;
        }
        public void SetEating(bool eating)
        {
            if (!_captured || faceRenderer == null) return;
            faceRenderer.sprite = eating && facePairs != null ? facePairs.EatingFor(_idle) : _idle;
        }
        public void EndPreview() { SetEating(false); _captured = false; }
        private void OnDisable() => EndPreview();
    }
}
