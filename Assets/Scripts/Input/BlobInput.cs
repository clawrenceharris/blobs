using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlobInput : MonoBehaviour
{
   

    private float offsetX = 0.5f;
    private float offsetY = 0.5f;
    private Blob blob;

    private Vector3 worldPoint;

    public delegate void OnBlobSelected(Blob blob);
    public static event OnBlobSelected onBlobSelected;

    public delegate void OnBlobDeselected(Blob blob);
    public static event OnBlobDeselected onBlobDeselected;


    private void Awake() => blob = GetComponent<Blob>();

    private bool IsBlobAtPosition(Vector3 worldPoint)
    {
        if(blob.Position != null)
        {

            return (int)worldPoint.x == blob.Position.x && (int)worldPoint.y == blob.Position.y;


        }
        return false;
    }

    private void CheckInput()
    {

        if (Input.GetMouseButtonDown(0))
        {
            worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            worldPoint = new Vector3(worldPoint.x + offsetX, worldPoint.y + offsetY);
            if (IsBlobAtPosition(worldPoint))
            {
                onBlobSelected?.Invoke(blob);

            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            worldPoint = new Vector3(worldPoint.x + offsetX, worldPoint.y + offsetY);

            if (IsBlobAtPosition(worldPoint))
            {

                onBlobDeselected?.Invoke(blob);

            }
        }
    }

    void Update()
    {
        CheckInput();

    }


   
}
