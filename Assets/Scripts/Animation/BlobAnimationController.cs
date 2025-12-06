using System;
using UnityEngine;

[RequireComponent(typeof(Animator))] // Ensures an Animator component is present
[RequireComponent(typeof(BlobView))]
public class BlobAnimationController : MonoBehaviour
{
    private Animator _animator;
    private BlobView _blobView;
    // --- Animator Parameter Names (Public for easy adjustment in Inspector if needed) ---
    // Using const strings for parameter names is a good practice to avoid typos
    public const string PARAM_IS_SELECTED = "IsSelected"; 
    public const string PARAM_TRIGGER_MERGE = "TriggerMerge";
    void Awake()
    {
        _blobView = GetComponent<BlobView>();
        _animator = GetComponent<Animator>();
    }
    private void Start()
    {
        BoardPresenter.OnBlobActivated += HandleBlobActivated;
        BoardPresenter.OnBlobDeactivated += HandleBlobDeactivated;

        BoardModel.OnBlobMoved +=  HandleBlobMoved;
    }

    private void OnDestroy()
    {
        BoardPresenter.OnBlobActivated -= HandleBlobActivated;
        BoardPresenter.OnBlobDeactivated -= HandleBlobDeactivated;
        BoardModel.OnBlobMoved -=  HandleBlobMoved;
    }

    private void HandleBlobMoved(Blob blob, Vector2Int int1, Vector2Int int2)
    {
        if (blob.ID != _blobView.Model.ID)
        {
            return;
        }
        TriggerMerge();
    }

    private void HandleBlobDeactivated(Blob blob)
    {
        if (blob.ID != _blobView.Model.ID)
        {
            return;
        }

        _animator.SetBool(PARAM_IS_SELECTED, false);
    }

    /// <summary>
    /// Sets the blob's selection state.
    /// </summary>
    /// <param name="isSelected">True to set to Selected state, False to return to Idle.</param>
    public void HandleBlobActivated(Blob blob)
    {
        
        
        if (blob.ID != _blobView.Model.ID)
        {
            return;
        }
        _animator.SetBool(PARAM_IS_SELECTED, true);
        
    }

    /// <summary>
    /// Triggers the merging animation for the blob.
    /// </summary>
    public void TriggerMerge()
    {
        if (_animator != null)
        {
            _animator.SetBool(PARAM_IS_SELECTED, false);

            _animator.SetTrigger(PARAM_TRIGGER_MERGE);
            
        }
    }

   
}