
using System.Collections;
using DG.Tweening;
using UnityEngine;


[RequireComponent(typeof(BlobVisuals))]
public class BlobView : MonoBehaviour
{
    public virtual Blob Model { get; protected set; }
    public string ID => Model.ID;

    public BlobVisuals Visuals { get; private set; }

    public T GetModelOfType<T>()
    where T : Blob
    {
        if (Model is T t)
        {
            return t;
        }
        return default;
    }
    public T OfType<T>()
    where T : BlobView
    {
        if (this is T t)
        {
            return t;

        }
        return default;
    }

    void Awake()
    {
        Visuals = GetComponent<BlobVisuals>();

    }
    
    public virtual void Setup(Blob model)
    {
        Model = model;
        transform.localScale = Vector3.zero;
        
        gameObject.name = $"{model.Type} Blob {model.GridPosition}";

    }


    public Vector2 GetScaleFromBlobSize() {
        return Model.Size switch
        {
            BlobSize.Small => Vector2.one * 0.5f,
            BlobSize.Big => Vector2.one * 1.4f,
            _ => Vector2.one,
        };
    }
    public virtual IEnumerator Spawn()
    {
        transform.DOScale(GetScaleFromBlobSize(), 0.3f);
        StartCoroutine(CreateParticles());
        yield return null;
    }
    public virtual IEnumerator StartMove()
    {
        ChangeSortingLayer("Foreground", transform);

        yield return null;
    }
    private void ChangeSortingLayer(string layerName, Transform transform)
    {
        foreach(Transform child in transform)
        {
            if (child.TryGetComponent<SpriteRenderer>(out var sr))
            {
                sr.sortingLayerID = SortingLayer.NameToID(layerName);
            }
            if(child.childCount > 0)
            {
                ChangeSortingLayer(layerName, child);
            }
        }
       
    }
    public virtual IEnumerator Merge()
    {
        StartCoroutine(CreateParticles());
        yield return null;
    }
    public virtual IEnumerator Remove(float duration)
    {
        StartCoroutine(CreateParticles());
        transform.DOScale(Vector3.zero, duration).SetEase(Ease.InBack);
        yield return null;
    }

    protected virtual IEnumerator CreateParticles()
    {
        float duration = 1f;
        ParticleSystem particles = Instantiate(Visuals.Particles, transform.position, Quaternion.identity);

        var main = particles.main;
        main.loop = false;

        main.startColor = ColorSchemeManager.FromBlobColor(Model.Color);
        particles.Play();
        yield return new WaitForSeconds(duration);
        Destroy(particles);

    }

    public virtual IEnumerator EndMove()
    {
        ChangeSortingLayer("Blobs", transform);
        yield return null;


    }
}
