using DG.Tweening;
using UnityEngine;

namespace Blobs.Presentation
{
    public sealed class FlagCaptureFeedback :
        MonoBehaviour,
        IBlobContactFeedback
    {
        [SerializeField] private Transform visualRoot;
        [SerializeField] private float punchScale = 0.15f;
        [SerializeField] private Animator animator;
        public void PlayContactFeedback(BlobContactFeedbackContext context)
        {
            Transform target = visualRoot != null ? visualRoot : transform;
            Vector3 punch = target.localScale * punchScale;

            target.DOPunchScale(
                   punch,
                   0.5f,
                   vibrato: 5,
                   elasticity: 0.5f)
                .OnComplete(() => animator.SetBool(Animator.StringToHash("IsVictory"), true));


        }

        public void PlayUndoFeedback(BlobContactFeedbackContext context)
        {
            if (animator == null) return;
            animator.SetBool(Animator.StringToHash("IsVictory"), false);
        }
    }
}
