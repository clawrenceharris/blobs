using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleBehavior : MonoBehaviour
{
    private ParticleSystem particles;

    void Awake(){
        particles = GetComponent<ParticleSystem>();
    }
    
   

    public void Create(Color32 color1, Color32 color2, float duration, bool loop = true, float gravity = 0){
        var main = particles.main;
        main.startColor = new ParticleSystem.MinMaxGradient(color1, color2);
         particles.Play();

        if (particles.isPlaying == false)
        {
            main.loop = loop;
            main.duration = duration;
            main.gravityModifier = gravity;
        }
        

        if(loop)
            Destroy(gameObject, duration);

    }

    
}
