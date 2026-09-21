using UnityEngine;

namespace Blobs.Presentation
{
    /// <summary>
    /// Swaps blob face sprites for the contact beat. Leave the impact sprite unassigned
    /// until a squint/closed-eye asset is dropped in.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BlobMergeExpressionFeedback : MonoBehaviour, IMergeImpactFeedback
    {
        [Tooltip("Closed-eye or impact face. Leave empty until the sprite is authored.")]
        [SerializeField] private Sprite _impactFace;

        public void PlayImpact(MergeImpactFeedbackContext context)
        {
            if (_impactFace == null)
                return;

            SetFace(context.Source, _impactFace);
            SetFace(context.Target, _impactFace);
        }

        public void PlaySettled(MergeImpactFeedbackContext context)
        {
            RestoreFace(context.Source);
            RestoreFace(context.Target);
        }

        private static void SetFace(BlobView blob, Sprite sprite)
        {
            SpriteRenderer face = FindFace(blob);
            if (face == null)
                return;

            CacheOriginal(face);
            face.sprite = sprite;
        }

        private static void RestoreFace(BlobView blob)
        {
            SpriteRenderer face = FindFace(blob);
            if (face == null)
                return;

            var cache = face.GetComponent<MergeFaceCache>();
            if (cache != null && cache.Original != null)
                face.sprite = cache.Original;
        }

        private static SpriteRenderer FindFace(BlobView blob)
        {
            if (blob == null)
                return null;

            Transform face = null;
            if (blob.VisualRoot != null)
                face = blob.VisualRoot.Find("Face");
            face ??= blob.transform.Find("VisualRoot/Face") ?? blob.transform.Find("Face");
            if (face == null)
            {
                foreach (Transform child in blob.GetComponentsInChildren<Transform>(true))
                {
                    if (child.name == "Face")
                    {
                        face = child;
                        break;
                    }
                }
            }

            return face != null ? face.GetComponent<SpriteRenderer>() : null;
        }

        private static void CacheOriginal(SpriteRenderer face)
        {
            MergeFaceCache cache = face.GetComponent<MergeFaceCache>();
            if (cache == null)
                cache = face.gameObject.AddComponent<MergeFaceCache>();
            if (cache.Original == null)
                cache.Original = face.sprite;
        }

        private sealed class MergeFaceCache : MonoBehaviour
        {
            public Sprite Original;
        }
    }
}
