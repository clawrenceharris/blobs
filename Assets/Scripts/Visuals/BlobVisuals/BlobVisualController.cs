using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public  class BlobVisualController : VisualController
{
    protected Blob blob;
    protected SpriteRenderer sprite;
    protected ParticleBehavior particles;
    protected BlobVisuals Visuals => GetVisuals<BlobVisuals>();
    protected override IVisuals visuals => Visuals;

    public override void Init(BlobsGameObject bObject)
    {



        sprite = blob.GetComponent<SpriteRenderer>();
        blob = (Blob)bObject;
    }
  
    public void MoveToFront(){
        sprite.sortingOrder += 100;
    }

     public void MoveToBack(){
        sprite.sortingOrder -= 100;
    }
   
    public virtual void DoMerge(float size){
        if (!blob.gameObject.activeSelf)
        {
            blob.gameObject.SetActive(true);
        }

        TweenScale(Vector3.one * size);
        CreateParticles();
    }
    



    protected float FromBlobSize()
    {
        return blob.Size switch
        {
            0 => 0,
            1 or 2 => BlobVisuals.smallSize,
            >= 3 =>BlobVisuals.bigSize,
           
            _ => 0,
        };
    }


   
    
    
    public void CreateParticles(float time = 1f){
        particles = Object.Instantiate(Visuals.particles, blob.transform.position, Quaternion.identity ).GetComponent<ParticleBehavior>();
        particles.Create(ColorSchemeManager.FromBlobColor(blob.Color), ColorSchemeManager.FromBlobColor(blob.Color), time);
    }

    public void TweenScale(Vector3 scale){
        blob.transform.DOScale(scale, 0.5f).SetEase(Ease.OutBack);
    }

    public void TweenMove(Vector3 targetPos){
        blob.transform.DOMove(targetPos, 0.8f).SetEase(Ease.InOutCubic);
        
    }

    public void TweenScale(GameObject gameObject, Vector3 scale){
        gameObject.transform.DOScale(scale,0.5f).SetEase(Ease.OutBack);
    }

    public void TweenMove(GameObject gameObject, Vector3 targetPos){
        gameObject.transform.DOMove(targetPos,0.8f).SetEase(Ease.InOutCubic);
        
    }

    
    public virtual void Remove(){
        TweenScale(Vector3.zero);
        CreateParticles();
    }
    
   
    
}
