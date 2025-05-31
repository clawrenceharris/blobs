using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserVisualController : TileVisualController
{
    Laser laser;
    public new LaserVisuals Visuals { get; private set; }
    ParticleBehavior particles;
    public override void Init(BlobsGameObject bObject)
    {
        base.Init(bObject);

        laser = (Laser)bObject;
        Visuals = laser.GetComponent<LaserVisuals>();
        CreateParticles(5f, false);
        
    }

    protected override void InitSprite(){
        if(laser.direction.x == 1)
            Visuals.SpriteRenderer.sprite = Visuals.horizontalLaserSprite;
        else if(laser.direction.y == 1)
            Visuals.SpriteRenderer.sprite = Visuals.verticalLaserSprite;

        Visuals.SpriteRenderer.color = ColorSchemeManager.FromBlobColor(laser.color);
        base.InitSprite();
    }


    public IEnumerator BlinkOff(int times){
        yield return new WaitForSeconds(0.5f);
        for(int i = 0; i < times; i++){
            
            Visuals.SpriteRenderer.enabled = false;
            yield return new WaitForSeconds(0.05f);
            Visuals.SpriteRenderer.enabled = true;
            yield return new WaitForSeconds(0.09f);
            Visuals.SpriteRenderer.enabled = false;
        }
        DeactivateLaserParticles();
        this.Visuals.SpriteRenderer.enabled = false;

        
    }

    public IEnumerator BlinkOn(int times)
    {
        yield return new WaitForSeconds(0.5f);
        Visuals.SpriteRenderer.enabled = true;
        for(int i = 0; i < times; i++)
        {
            yield return new WaitForSeconds(0.09f);
            Visuals.SpriteRenderer.enabled = false;
            yield return new WaitForSeconds(0.05f);
            Visuals.SpriteRenderer.enabled = true;
        }
        ActivateLaserParticles();
        
    }

    public void ActivateLaserParticles()
    {
        CreateParticles(3f, false);
    }
    public void DeactivateLaserParticles()
    {
        CreateParticles(1f, false, 0.5f);
    }

    private void CreateParticles(float duration = 1f, bool loop = true, float gravity = 0)
    {
        particles = Object.Instantiate(Visuals.particles, laser.transform.position, Quaternion.identity ).GetComponent<ParticleBehavior>();
        particles.Create(ColorSchemeManager.FromBlobColor(laser.color), ColorSchemeManager.FromBlobColor(laser.color), duration, loop, gravity);
    }


   
}
