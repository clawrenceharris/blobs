using Blobs.Application;
using Blobs.Core;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Blobs.Input
{
    public sealed class GameplayInputAdapter : MonoBehaviour
    {
        private IGameplayCommands _commands;
        [SerializeField] private InputActionReference selectBlobAction;
        [SerializeField] private Camera boardCamera;
        [SerializeField, Min(0.01f)] private float cellSize = 1f;
        [SerializeField] private Vector2 boardOrigin;
        private InputAction _runtimePointAction;
        private bool _subscribed;
        public event Action<BlobSelectionResult> BlobSelectionResolved;

        private InputAction SelectBlobAction =>
            selectBlobAction != null ? selectBlobAction.action : _runtimePointAction;

        public void Initialize(IGameplayCommands commands, float boardCellSize)
        {
            UnsubscribeInputActions();
            _commands = commands;
            cellSize = boardCellSize;
            EnsureRuntimeAction();
            Subscribe();
        }

        public BlobSelectionResult SelectBlobAt(GridPosition gridPosition)
        {
            if (_commands == null)
                return BlobSelectionResult.Cleared();

            return _commands.SelectBlobAt(gridPosition);
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
                action.performed += OnSelectBlob;
                _subscribed = true;
            }

            action.Enable();
        }

        private void UnsubscribeInputActions()
        {
            var action = SelectBlobAction;
            if (action != null && _subscribed)
            {
                action.performed -= OnSelectBlob;
                action.Disable();
            }

            _subscribed = false;
        }

        private void DisposeRuntimeAction()
        {
            _runtimePointAction?.Dispose();
            _runtimePointAction = null;
        }

        private void OnSelectBlob(InputAction.CallbackContext context)
        {
            if (!context.performed || _commands == null)
                return;

            if (!TryGetPointerScreenPosition(out Vector2 screenPosition))
                return;

            Camera cameraToUse = boardCamera != null ? boardCamera : Camera.main;
            if (cameraToUse == null)
                return;

            Ray ray = cameraToUse.ScreenPointToRay(screenPosition);
            var boardPlane = new Plane(Vector3.forward, Vector3.zero);
            if (!boardPlane.Raycast(ray, out float distance))
                return;

            Vector3 worldPosition = ray.GetPoint(distance);
            var gridPosition = new GridPosition(
                Mathf.RoundToInt((worldPosition.x - boardOrigin.x) / cellSize),
                Mathf.RoundToInt((worldPosition.y - boardOrigin.y) / cellSize));

            SelectBlobAt(gridPosition);

        }

        private static bool TryGetPointerScreenPosition(out Vector2 screenPosition)
        {
            if (Pointer.current != null)
            {
                screenPosition = Pointer.current.position.ReadValue();
                return true;
            }

            screenPosition = default;
            return false;
        }
    }
}
