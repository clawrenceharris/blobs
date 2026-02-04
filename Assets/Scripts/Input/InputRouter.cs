using UnityEngine;

namespace Blobs.Input
{
    /// <summary>
    /// Routes click and swipe input using 2D colliders and raycasting.
    /// Click: raycast from pointer hits tile/blob colliders; blob at that cell is selected (or empty click).
    /// Swipe: pointer down on blob A, pointer up on blob B with A != B invokes merge from A to B.
    /// </summary>
    public class InputRouter : MonoBehaviour
    {
        [SerializeField] private KeyCode undoKey = KeyCode.Z;
        [SerializeField] private InputGate gate;
        [SerializeField] private LayerMask _boardLayerMask = -1;
        [SerializeField] private float _rayDistance = 1000f;

        public static event System.Action<BlobView> BlobClicked;
        public static event System.Action EmptyClicked;
        public static event System.Action UndoPressed;

        private Camera _cam;
        private BlobView _pointerDownBlob;
        private IBoardPresenter _board;

        private void Awake()
        {
            _cam = Camera.main;
            gate = gate == null ? FindFirstObjectByType<InputGate>() : gate;
            _board = FindFirstObjectByType<BoardPresenter>();
        }

        private void Update()
        {
            if (!gate.Enabled) return;

            if (UnityEngine.Input.GetKeyDown(undoKey)) UndoPressed?.Invoke();

            if (UnityEngine.Input.GetMouseButtonDown(0))
                RecordPointerDown();
            else if (UnityEngine.Input.GetMouseButtonUp(0))
                HandlePointerUp();
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

            if (!hitBoard)
            {
                if (_pointerDownBlob != null)
                    BlobClicked?.Invoke(_pointerDownBlob);
                else
                    EmptyClicked?.Invoke();
                return;
            }

            if (_pointerDownBlob != null && endBlobView != null &&
                _pointerDownBlob.Model != null && endBlobView.Model != null &&
                _pointerDownBlob.Model.ID != endBlobView.Model.ID)
            {
                BlobClicked?.Invoke(_pointerDownBlob);
                BlobClicked?.Invoke(endBlobView);
                return;
            }

            if (_pointerDownBlob != null && (endBlobView == null || _pointerDownBlob.Model?.ID == endBlobView.Model?.ID))
            {
                BlobClicked?.Invoke(_pointerDownBlob);
                return;
            }

            if (_pointerDownBlob == null && endBlobView != null)
            {
                BlobClicked?.Invoke(endBlobView);
                return;
            }

            EmptyClicked?.Invoke();
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

            Ray ray = _cam.ScreenPointToRay(UnityEngine.Input.mousePosition);
            RaycastHit2D hit = Physics2D.GetRayIntersection(ray, _rayDistance, _boardLayerMask);
            if (!hit.collider) return false;

            GameObject go = hit.collider.gameObject;

            BlobView blobView = go.GetComponent<BlobView>() ?? go.GetComponentInParent<BlobView>();
            if (blobView != null)
            {
                blob = blobView;
                hitBoard = true;
                return true;
            }

            TileView tileView = go.GetComponent<TileView>() ?? go.GetComponentInParent<TileView>();
            if (tileView != null && tileView.Model != null)
            {
                hitBoard = true;
                var presenter = _board.GetBlobAt(tileView.Model.GridPosition);
                blob = presenter?.View;
                return true;
            }

            return false;
        }
    }
}