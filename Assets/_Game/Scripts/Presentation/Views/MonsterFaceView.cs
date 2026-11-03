using Blobs.Core;
using UnityEngine;

namespace Blobs.Presentation
{
    /// <summary>
    /// Owns which authored face variant is visible on a monster. Exactly one variant is active
    /// at a time; idle is restored whenever no eating or victory beat is playing.
    /// Facing is expressed by swapping face GameObjects (not rotating the billboard sprite).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MonsterFaceView : MonoBehaviour
    {
        [SerializeField] private GameObject idle;
        [SerializeField, Tooltip("Optional idle looking left. Falls back to the side eating face, then idle.")]
        private GameObject idleLeft;
        [SerializeField, Tooltip("Optional idle looking right. Falls back to the side eating face, then idle.")]
        private GameObject idleRight;
        [SerializeField, Tooltip("Optional idle looking up. Falls back to the up eating face, then idle.")]
        private GameObject idleUp;
        [SerializeField] private GameObject eatingFront;
        [SerializeField] private GameObject eatingLeft;
        [SerializeField] private GameObject eatingRight;
        [Tooltip("Optional. Falls back to the front eating face when empty.")]
        [SerializeField] private GameObject eatingUp;
        [Tooltip("Optional contented face (closed eyes + smile) used after nest capture. Falls back to idle.")]
        [SerializeField] private GameObject victory;

        private GameObject _active;
        private Direction _facing = Direction.South;
        private bool _locked;

        private void Awake() => ShowIdle();

        /// <summary>Authored or runtime look direction used by <see cref="ShowIdle"/>.</summary>
        public Direction Facing => _facing;

        /// <summary>
        /// Sets the resting look direction and refreshes the idle face unless a one-shot face is locked.
        /// </summary>
        public void SetFacing(Direction direction)
        {
            _facing = direction;
            if (!_locked)
                ShowIdle();
        }

        public void ShowIdle()
        {
            _locked = false;
            Show(ResolveIdle(_facing));
        }

        /// <summary>Shows the eating face that opens toward the prey, in world/grid space.</summary>
        public void ShowEating(Vector2 towardPrey)
        {
            _locked = true;
            Show(ResolveEating(towardPrey));
        }

        /// <summary>Shows the nest-home victory face. Falls back to idle when unauthored.</summary>
        public void ShowVictory()
        {
            _locked = true;
            Show(victory != null ? victory : idle);
        }

        /// <summary>Hides every face variant. Used by nest hosts that should not wear a face.</summary>
        public void Hide()
        {
            _locked = true;
            SetActive(idle, false);
            SetActive(idleLeft, false);
            SetActive(idleRight, false);
            SetActive(idleUp, false);
            SetActive(eatingFront, false);
            SetActive(eatingLeft, false);
            SetActive(eatingRight, false);
            SetActive(eatingUp, false);
            SetActive(victory, false);
            _active = null;
        }

        private GameObject ResolveIdle(Direction facing)
        {
            switch (facing)
            {
                case Direction.East:
                    return FirstAvailable(idleRight, eatingRight, idle);
                case Direction.West:
                    return FirstAvailable(idleLeft, eatingLeft, idle);
                case Direction.North:
                    return FirstAvailable(idleUp, eatingUp, idle);
                default:
                    return idle;
            }
        }

        private GameObject ResolveEating(Vector2 towardPrey)
        {
            GameObject face;
            if (Mathf.Abs(towardPrey.x) > Mathf.Abs(towardPrey.y))
                face = towardPrey.x > 0f ? eatingRight : eatingLeft;
            else
                face = towardPrey.y > 0f ? eatingUp : eatingFront;
            return face != null ? face : eatingFront;
        }

        private static GameObject FirstAvailable(params GameObject[] faces)
        {
            for (int i = 0; i < faces.Length; i++)
            {
                if (faces[i] != null)
                    return faces[i];
            }

            return null;
        }

        private void Show(GameObject face)
        {
            if (face == null)
                face = idle;
            if (face == _active && face != null && face.activeSelf)
                return;

            SetActive(idle, face == idle);
            SetActive(idleLeft, face == idleLeft);
            SetActive(idleRight, face == idleRight);
            SetActive(idleUp, face == idleUp);
            SetActive(eatingFront, face == eatingFront);
            SetActive(eatingLeft, face == eatingLeft);
            SetActive(eatingRight, face == eatingRight);
            SetActive(eatingUp, face == eatingUp);
            SetActive(victory, face == victory);
            _active = face;
        }

        private static void SetActive(GameObject face, bool active)
        {
            if (face != null && face.activeSelf != active)
                face.SetActive(active);
        }
    }
}
