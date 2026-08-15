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
            Transform target =
                visualRoot != null ? visualRoot : transform;

            target.DOKill();

            return target.DOPunchScale(
                Vector3.one * punchScale,
                duration,
                vibrato: 6,
                elasticity: 0.6f);
        }

        private void OnDestroy()
        {
            if (visualRoot != null)
                visualRoot.DOKill();

            transform.DOKill();
        }
    }
}