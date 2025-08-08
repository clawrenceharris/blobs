using System.Collections;
using DG.Tweening;
using UnityEngine;

public class GhostBlobView : BlobView
{
    protected override IEnumerator CreateParticles()
    {
        float duration = 1f;
        ParticleSystem particles = Instantiate(Visuals.Particles, transform.position, Quaternion.identity);

        var main = particles.main;
        main.loop = false;

        main.startColor = Color.white;
        particles.Play();
        yield return new WaitForSeconds(duration);
        Destroy(particles);
    }

    public override IEnumerator StartMove()
    {
        Visuals.SpriteRenderer.DOFade(0, 0.3f);
        yield return null;
    }
    public override IEnumerator Merge()
    {
        StartCoroutine(base.Merge());
        yield return Visuals.SpriteRenderer.DOFade(1, 0.3f).WaitForCompletion();
        
    }
}