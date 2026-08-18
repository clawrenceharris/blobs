using DG.Tweening;
using UnityEngine;

namespace Blobs.Presentation
{
    public sealed class FlagCaptureFeedback :
        MonoBehaviour,
        IMergeTargetFeedback
    {
        [SerializeField] private Transform visualRoot;
        [SerializeField] private float punchScale = 0.15f;
        public Tween PlaySourceAccepted(float duration)
        {
            Transform target = visualRoot != null ? visualRoot : transform;
            Vector3 punch = target.localScale * punchScale;
            return target.DOPunchScale(
                punch,
                Mathf.Max(0.01f, duration),
                vibrato: 5,
                elasticity: 0.5f);
        }

        private void OnDestroy()
        {
            if (visualRoot != null)
                visualRoot.DOKill();

            transform.DOKill();
        }
    }
}
