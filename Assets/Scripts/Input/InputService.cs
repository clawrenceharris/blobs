using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Blobs.Input
{
    /// <summary>
    /// Routes click and swipe input using 2D colliders and raycasting.
    /// Click: raycast from pointer hits tile/blob colliders; blob at that cell is selected (or empty click).
    /// Swipe: pointer down on blob A, pointer up on blob B with A != B invokes merge from A to B.
    /// </summary>
    public class InputService : MonoBehaviour
    {
        [SerializeField] private KeyCode undoKey = KeyCode.Z;
        [SerializeField] private LayerMask _boardLayerMask = -1;
        [SerializeField] private float _rayDistance = 1000f;
        private static InputGate _gate;
        public static InputGate Gate => _gate ??= new InputGate();
        public static event Action<BlobView> BlobClicked;
        public static event Action EmptyClicked;
        public static event Action UndoPressed;

        private Camera _cam;
        private BlobView _pointerDownBlob;
        private IBoardPresenter _board;

        private void Awake()
        {
            _cam = Camera.main;
            _board = FindFirstObjectByType<BoardPresenter>();
        }

        private void Update()
        {
            if (!Gate.Enabled) return;

            if (IsUndoPressed())
            {
                UndoPressed?.Invoke();
            }

            if (IsPointerPressDown())
            {
                RecordPointerDown();
            }
            else if (IsPointerPressUp())
            {
                HandlePointerUp();
            }
        }

        private void RecordPointerDown()
        {
            _pointerDownBlob = null;

            if (!TryGetPointerHit(out BlobView blob, out bool hitBoard)) return;
            _pointerDownBlob = blob;
        }

        private void HandlePointerUp()
        {
            TryGetPointerHit(out BlobView endBlobView, out bool hitBoard);

            // Validate stale _pointerDownBlob reference (blob may have been
            // destroyed or deactivated during a merge animation)
            if (_pointerDownBlob != null &&
                (_pointerDownBlob.gameObject == null || !_pointerDownBlob.gameObject.activeInHierarchy
                 || _pointerDownBlob.Model == null))
            {
                _pointerDownBlob = null;
            }

            if (!hitBoard)
            {
                _pointerDownBlob = null;
                EmptyClicked?.Invoke();
                return;
            }

            if (_pointerDownBlob != null && endBlobView != null
                && _pointerDownBlob.Model != null && endBlobView.Model != null
                && _pointerDownBlob.Model.ID != endBlobView.Model.ID)
            {
                BlobClicked?.Invoke(_pointerDownBlob);
                BlobClicked?.Invoke(endBlobView);
                _pointerDownBlob = null;
                return;
            }
            // if the blob is the same 
            if (_pointerDownBlob != null && _pointerDownBlob.Model != null
                && (endBlobView == null || endBlobView.Model == null
                    || _pointerDownBlob.Model.ID == endBlobView.Model.ID))
            {
                BlobClicked?.Invoke(_pointerDownBlob);
                _pointerDownBlob = null;
                return;
            }

            _pointerDownBlob = null;
            EmptyClicked?.Invoke();
        }


        private bool IsUndoPressed()
        {
            if (Keyboard.current == null) return false;
            if (!TryMapKeyCode(undoKey, out Key key)) return false;
            return Keyboard.current[key].wasPressedThisFrame;
        }

        private bool IsPointerPressDown()
        {
            return Pointer.current != null && Pointer.current.press.wasPressedThisFrame;
        }

        private bool IsPointerPressUp()
        {
            return Pointer.current != null && Pointer.current.press.wasReleasedThisFrame;
        }

        private bool TryGetPointerScreenPosition(out Vector2 position)
        {
            position = Vector2.zero;
            if (Pointer.current == null) return false;
            position = Pointer.current.position.ReadValue();
            return true;
        }

        private static bool TryMapKeyCode(KeyCode keyCode, out Key key)
        {
            if (Enum.TryParse(keyCode.ToString(), out key))
                return true;

            switch (keyCode)
            {
                case KeyCode.Alpha0: key = Key.Digit0; return true;
                case KeyCode.Alpha1: key = Key.Digit1; return true;
                case KeyCode.Alpha2: key = Key.Digit2; return true;
                case KeyCode.Alpha3: key = Key.Digit3; return true;
                case KeyCode.Alpha4: key = Key.Digit4; return true;
                case KeyCode.Alpha5: key = Key.Digit5; return true;
                case KeyCode.Alpha6: key = Key.Digit6; return true;
                case KeyCode.Alpha7: key = Key.Digit7; return true;
                case KeyCode.Alpha8: key = Key.Digit8; return true;
                case KeyCode.Alpha9: key = Key.Digit9; return true;
                case KeyCode.LeftControl: key = Key.LeftCtrl; return true;
                case KeyCode.RightControl: key = Key.RightCtrl; return true;
                case KeyCode.LeftShift: key = Key.LeftShift; return true;
                case KeyCode.RightShift: key = Key.RightShift; return true;
                case KeyCode.LeftAlt: key = Key.LeftAlt; return true;
                case KeyCode.RightAlt: key = Key.RightAlt; return true;
            }

            return false;
        }


        /// <summary>
        /// Raycasts from the pointer into the scene. Returns the blob under the pointer (if any) and whether we hit the board (tile or blob).
        /// Hit board + blob = blob at that cell; hit board + no blob = empty tile; no hit = off board.
        /// </summary>
        private bool TryGetPointerHit(out BlobView blob, out bool hitBoard)
        {
            blob = null;
            hitBoard = false;
            if (_cam == null || _board == null) return false;
            if (!TryGetPointerScreenPosition(out Vector2 pointerPosition)) return false;
            Ray ray = _cam.ScreenPointToRay(pointerPosition);
            RaycastHit2D hit = Physics2D.GetRayIntersection(ray, _rayDistance, _boardLayerMask);
            if (!hit.collider)
            {
                return false;
            }
            ;

            GameObject go = hit.collider.gameObject;

            if (go.TryGetComponent<BlobView>(out var blobView))
            {
                blob = blobView;
                hitBoard = true;
                return true;
            }

            return false;
        }
    }


}