using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Object pool for BlobView instances. Tracks by blob ID for undo reuse, and by BlobType for generic reuse.
/// </summary>
public class BlobViewPool : MonoBehaviour
{
    /// <summary>ID -> view for exact reuse on undo (same blob ID returns same view).</summary>
    private readonly Dictionary<string, BlobView> _idPool = new();

    /// <summary>Type -> stack for generic reuse when no ID match.</summary>
    private readonly Dictionary<BlobType, Stack<BlobView>> _typePools = new();

    private Transform _poolRoot;

    private void Awake()
    {
        _poolRoot = new GameObject("BlobViewPool").transform;
        _poolRoot.SetParent(transform);
        _poolRoot.localPosition = Vector3.zero;
    }

    /// <summary>
    /// Get a BlobView by blob ID. Returns the previously released view for this ID if available (e.g. undo).
    /// Returns null if not found; caller should fall back to Get(type, parent, worldPosition).
    /// </summary>
    public BlobView GetById(string blobId, Transform parent, Vector3 worldPosition)
    {
        if (string.IsNullOrEmpty(blobId) || !_idPool.TryGetValue(blobId, out var view))
            return null;

        _idPool.Remove(blobId);
        view.transform.SetParent(parent);
        view.transform.position = worldPosition;
        view.gameObject.SetActive(true);
        return view;
    }

    /// <summary>
    /// Get a BlobView for the given type. Tries type pool first, then instantiates from PrefabLibrary.
    /// Caller must call Initialize(blob) and set parent/position as needed.
    /// </summary>
    public BlobView Get(BlobType type, Transform parent, Vector3 worldPosition)
    {
        // Try type-based stack first
        if (_typePools.TryGetValue(type, out var stack) && stack.Count > 0)
        {
            var pooled = stack.Pop();
            if (pooled != null)
                _idPool.Remove(pooled.ID); // same view may be in both; remove from ID pool when handing out
            if (pooled != null)
            {
                pooled.transform.SetParent(parent);
                pooled.transform.position = worldPosition;
                pooled.gameObject.SetActive(true);
                return pooled;
            }
        }

        var prefab = PrefabLibrary.Instance.FromBlobType(type);
        var view = Instantiate(prefab, worldPosition, Quaternion.identity, parent);
        return view;
    }

    /// <summary>
    /// Return a view to the pool. Stored by ID when Model is set (for undo), and by type for generic reuse.
    /// </summary>
    public void Release(BlobView view)
    {
        if (view == null) return;

        view.gameObject.SetActive(false);
        view.transform.SetParent(_poolRoot);

        if (view != null && !string.IsNullOrEmpty(view.ID))
            _idPool[view.ID] = view;

        var type = view.Type;
        if (!_typePools.TryGetValue(type, out var stack))
        {
            stack = new Stack<BlobView>();
            _typePools[type] = stack;
        }
        stack.Push(view);
    }

    /// <summary>
    /// Release all pooled views (e.g. on board clear).
    /// </summary>
    public void ClearAll()
    {
        _idPool.Clear();
        foreach (var stack in _typePools.Values)
        {
            while (stack.Count > 0)
            {
                var view = stack.Pop();
                if (view != null)
                    Destroy(view.gameObject);
            }
        }
    }
}
