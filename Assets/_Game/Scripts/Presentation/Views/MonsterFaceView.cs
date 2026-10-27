using UnityEngine;

namespace Blobs.Presentation
{
    /// <summary>
    /// Owns which authored face variant is visible on a monster. Exactly one variant is active
    /// at a time; the idle face is restored whenever no eating beat is playing.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MonsterFaceView : MonoBehaviour
    {
        [SerializeField] private GameObject idle;
        [SerializeField] private GameObject eatingFront;
        [SerializeField] private GameObject eatingLeft;
        [SerializeField] private GameObject eatingRight;
        [Tooltip("Optional. Falls back to the front eating face when empty.")]
        [SerializeField] private GameObject eatingUp;

        private GameObject _active;

        private void Awake() => ShowIdle();

        public void ShowIdle() => Show(idle);

        /// <summary>Shows the eating face that opens toward the prey, in world/grid space.</summary>
        public void ShowEating(Vector2 towardPrey) => Show(ResolveEating(towardPrey));

        private GameObject ResolveEating(Vector2 towardPrey)
        {
            GameObject face;
            if (Mathf.Abs(towardPrey.x) > Mathf.Abs(towardPrey.y))
                face = towardPrey.x > 0f ? eatingRight : eatingLeft;
            else
                face = towardPrey.y > 0f ? eatingUp : eatingFront;
            return face != null ? face : eatingFront;
        }

        private void Show(GameObject face)
        {
            if (face == null)
                face = idle;
            if (face == _active && face != null && face.activeSelf)
                return;

            SetActive(idle, face == idle);
            SetActive(eatingFront, face == eatingFront);
            SetActive(eatingLeft, face == eatingLeft);
            SetActive(eatingRight, face == eatingRight);
            SetActive(eatingUp, face == eatingUp);
            _active = face;
        }

        private static void SetActive(GameObject face, bool active)
        {
            if (face != null && face.activeSelf != active)
                face.SetActive(active);
        }
    }
}
