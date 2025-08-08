
using System.Collections;
using DG.Tweening;
using UnityEngine;


[RequireComponent(typeof(BlobVisuals))]
public class BlobView : MonoBehaviour
{
    public virtual Blob Model { get; protected set; }
    public string ID => Model.ID;
    public BlobInput Input { get; private set; }

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
        Input = GetComponent<BlobInput>();

    }
    
    // The Presenter calls this to link the View to its data Model.
    public virtual void Setup(Blob blob)
    {
        Model = blob;
        transform.localScale = Vector3.zero;
        
        // Configure the visuals based on the data.
        gameObject.name = $"{blob.Type} Blob {blob.GridPosition}";

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
        yield return null;
        StartCoroutine(CreateParticles());
    }
    public virtual IEnumerator StartMove()
    {
        yield break;
    }
    public virtual IEnumerator Merge()
    {
        yield return null;
        StartCoroutine(CreateParticles());
    }
    public virtual IEnumerator Remove(float duration)
    {
        yield return null;
        StartCoroutine(CreateParticles());
        transform.DOScale(Vector3.zero, duration).SetEase(Ease.InBack);
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
        yield break;
    }
}
