using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class BlobVisuals : MonoBehaviour, IVisuals
{
    public SpriteRenderer SpriteRenderer;
    public SpriteRenderer Highlight;
    public ParticleSystem Particles;

    public void ChangeSortingLayer(string layerName, Transform transform)
    {
        foreach (Transform child in transform)
        {
            if (child.TryGetComponent<SpriteRenderer>(out var sr))
            {
                sr.sortingLayerID = SortingLayer.NameToID(layerName);
            }
            if (child.childCount > 0)
            {
                ChangeSortingLayer(layerName, child);
            }
        }

    }
}
