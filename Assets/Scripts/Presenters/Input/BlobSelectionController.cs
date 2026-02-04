using System;
using System.Collections;
using Blobs.Input;
using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(InputRouter))]
[RequireComponent(typeof(BlobView))]
public class BlobSelectionController : MonoBehaviour
{
    private BlobView _blobView;
    private bool _isSelected = false;
    private Vector3 _initialScale;
    void Awake()
    {
        _blobView = GetComponent<BlobView>();

    }

    void Start()
    {
        _initialScale = _blobView.transform.localScale;
        InputRouter.EmptyClicked += HandleEmptyClicked;
        InputRouter.BlobClicked += HandleBlobClicked;
    }

    private void HandleBlobClicked(BlobView view)
    {
        if(view == _blobView)
        {
            _isSelected = true;
            StartCoroutine(PulseAnimation());
        }
            

    }
    private IEnumerator PulseAnimation()
    {
        Tween tween = _blobView.transform.DOPunchScale(_blobView.transform.localScale * 1.2f, 0.3f);
        yield return tween.WaitForCompletion();
    }
    private void HandleEmptyClicked()
    {
        if (_isSelected)
        {
            StopCoroutine(PulseAnimation());
            _blobView.transform.localScale = _initialScale;

        }

    }
}