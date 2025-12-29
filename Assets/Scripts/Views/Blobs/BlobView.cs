
using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;


[RequireComponent(typeof(BlobVisuals))]
public class BlobView : MonoBehaviour
{
    public virtual Blob Model { get; protected set; }
    public BlobInput Input;

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
        if (TryGetComponent<BlobInput>(out var input)) {
            Input = input;
        }
    }

    public virtual void Setup(Blob model)
    {
        Model = model;
        transform.localScale = Vector3.zero;

        gameObject.name = $"{model.Type} Blob {model.GridPosition}";

    }


    public virtual IEnumerator CreateParticles()
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

    public virtual IEnumerator Scale(float duration)
    {
        Vector3 scale = Vector2.one * Model.GetScaleFromBlobSize();
        transform.DOScale(scale, duration).WaitForCompletion();
        yield return null;
    }
}
