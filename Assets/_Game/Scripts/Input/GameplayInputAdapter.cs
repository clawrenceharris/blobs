using Blobs.Application;
using Blobs.Core;
using System;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem.Controls;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Blobs.Input
{
    /// <summary>
    /// Converts pointer drags into source-to-target gameplay commands. This adapter maps screen/world input
    /// to grid positions but does not update board visuals directly.
    /// </summary>
    public sealed class GameplayInputAdapter : MonoBehaviour
    {
        private IGameplayCommands _commands;
        [SerializeField] private InputActionReference selectBlobAction;
        [SerializeField] private Camera boardCamera;
        [SerializeField, Min(0.01f)] private float cellSize = 1f;
        [SerializeField] private Vector2 boardOrigin;
        private InputAction _runtimePointAction;
        private bool _subscribed;
        private InputControl _dragControl;
        private readonly List<RaycastResult> _uiHits = new();
        public event Action<BlobSelectionResult> BlobSelectionResolved;

        private InputAction SelectBlobAction =>
            selectBlobAction != null ? selectBlobAction.action : _runtimePointAction;

        /// <summary>
        /// Connects pointer input to the active gameplay command surface.
        /// </summary>
        public void Initialize(IGameplayCommands commands, float boardCellSize)
        {
            UnsubscribeInputActions();
            _commands = commands;
            cellSize = boardCellSize;
            EnsureRuntimeAction();
            Subscribe();
        }

        /// <summary>
        /// Testable entry point for selecting a logical grid position without hardware input.
        /// </summary>
        public BlobSelectionResult SelectBlobAt(GridPosition gridPosition)
        {
            if (_commands == null)
                return BlobSelectionResult.Cleared();

            BlobSelectionResult result = _commands.SelectBlobAt(gridPosition);
            BlobSelectionResolved?.Invoke(result);
            return result;
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            UnsubscribeInputActions();
        }

        private void OnDestroy()
        {
            UnsubscribeInputActions();
            DisposeRuntimeAction();
            _commands = null;
        }

        private void EnsureRuntimeAction()
        {
            if (selectBlobAction != null)
                return;

            if (_runtimePointAction != null)
                return;

            _runtimePointAction = new InputAction(
                "SelectBlob",
                InputActionType.Button,
                "<Pointer>/press");
        }

        private void Subscribe()
        {
            if (_commands == null)
                return;

            EnsureRuntimeAction();
            var action = SelectBlobAction;
            if (action == null)
                return;

            if (!_subscribed)
            {
                action.performed += OnPointerPress;
                action.canceled += OnPointerPress;
                _subscribed = true;
            }

            action.Enable();
        }

        private void UnsubscribeInputActions()
        {
            CancelDrag();
            var action = SelectBlobAction;
            if (action != null && _subscribed)
            {
                action.performed -= OnPointerPress;
                action.canceled -= OnPointerPress;
                action.Disable();
            }

            _subscribed = false;
        }

        private void DisposeRuntimeAction()
        {
            _runtimePointAction?.Dispose();
            _runtimePointAction = null;
        }

        private void CancelDrag()
        {
            _dragControl = null;
            _commands?.CancelDrag();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) CancelDrag();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) CancelDrag();
        }

        private void OnPointerPress(InputAction.CallbackContext context)
        {
            if (_commands == null) return;
            bool pressed = context.ReadValueAsButton();
            if (pressed && _dragControl != null) return;
            if (!pressed && _dragControl != context.control) return;

            TouchControl touch = context.control.parent as TouchControl;
            Vector2 screenPosition;
            if (touch != null)
            {
                if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Canceled)
                {
                    CancelDrag();
                    return;
                }
                screenPosition = touch.position.ReadValue();
            }
            else if (context.control.device is Pointer pointer)
                screenPosition = pointer.position.ReadValue();
            else
            {
                CancelDrag();
                return;
            }

            if (IsOverUi(screenPosition) || !TryGetGridPosition(screenPosition, out GridPosition position))
            {
                CancelDrag();
                return;
            }

            if (pressed)
            {
                BlobSelectionResult result = _commands.BeginDragAt(position);
                _dragControl = result.HasSelection ? context.control : null;
                BlobSelectionResolved?.Invoke(result);
            }
            else
            {
                _dragControl = null;
                BlobSelectionResult result = _commands.EndDragAt(position);
                BlobSelectionResolved?.Invoke(result);
            }
        }

        private bool IsOverUi(Vector2 screenPosition)
        {
            if (EventSystem.current == null) return false;
            // Raycast now: the UI module may not have processed this input event yet.
            var pointerData = new PointerEventData(EventSystem.current) { position = screenPosition };
            _uiHits.Clear();
            EventSystem.current.RaycastAll(pointerData, _uiHits);
            foreach (RaycastResult hit in _uiHits)
                if (hit.module is GraphicRaycaster) return true;
            return false;
        }

        private bool TryGetGridPosition(Vector2 screenPosition, out GridPosition gridPosition)
        {
            gridPosition = default;
            Camera cameraToUse = boardCamera != null ? boardCamera : Camera.main;
            if (cameraToUse == null || cellSize <= 0f || !cameraToUse.pixelRect.Contains(screenPosition))
                return false;

            Ray ray = cameraToUse.ScreenPointToRay(screenPosition);
            var boardPlane = new Plane(Vector3.forward, Vector3.zero);
            if (!boardPlane.Raycast(ray, out float distance)) return false;

            Vector3 worldPosition = ray.GetPoint(distance);
            gridPosition = new GridPosition(
                Mathf.RoundToInt((worldPosition.x - boardOrigin.x) / cellSize),
                Mathf.RoundToInt((worldPosition.y - boardOrigin.y) / cellSize));
            return true;
        }
    }
}
