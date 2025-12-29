using System.Collections;
using DG.Tweening;
using UnityEngine;

public class GhostBlobView : BlobView
{
    public override IEnumerator CreateParticles()
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
}