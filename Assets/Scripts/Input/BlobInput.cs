using System;
using UnityEngine;

[RequireComponent(typeof(BlobView))]
[RequireComponent(typeof(Collider2D))]

public class BlobInput : MonoBehaviour
{



    public static bool InputEnabled;
    public Action<Blob> OnBlobSelected;
    private BlobView _view;
    private void Awake() => _view = GetComponent<BlobView>();
    private void Start()
    {
        EnableInput();
    }

    public static void EnableInput()
    {
        InputEnabled = true;
    }

    public static void DisableInput()
    {
        InputEnabled = false;
    }

    private void OnMouseUp()
    {
        if (!_view.Model.Enabled) return;

        OnBlobSelected?.Invoke(_view.Model);
    }
    


}
