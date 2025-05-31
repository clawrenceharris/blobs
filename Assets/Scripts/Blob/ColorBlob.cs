using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ColorBlob : Blob
{


    public override bool CanMoveTo(Blob blobToMoveTo){
        bool isValid = true;
        if (blobToMoveTo.Color == Color){
            

            isValid = false;
        }

        if(Position.x != blobToMoveTo.Position.x && Position.y != blobToMoveTo.Position.y){
            isValid = false;
        }
        return isValid;
        
    }

    
    public override void DoMove(Position position)
    {
        base.DoMove(position);
        if (gameObject.activeSelf)
            StartCoroutine(MoveCo());

    }

    public override void Remove()
    {
        base.Remove();
        GetVisualController<ColorBlobVisualController>().Remove();
    }

    public IEnumerator MoveCo()
    {
        Vector3 targetPos = new(Position.x, Position.y);
        GetVisualController<ColorBlobVisualController>().MoveToFront(); 
        GetVisualController<ColorBlobVisualController>().TweenMove(targetPos);
        yield return new WaitForSeconds(Visuals.moveTime / 3);
        GetVisualController<ColorBlobVisualController>().MoveToBack();
    }


    public override void DoMerge(int size = 0)
    {
        Size += size;
        GetVisualController<ColorBlobVisualController>().OnBlobMerged();


    }

    //public void OnHStripe(){
    //    visualController.OnHStripe();
    //}

    //public void OnVStripe(){
    //    visualController.OnVStripe();
    //}

    //public void OnTStripe(){
    //    visualController.OnTStripe();
    //}

    //public void OnNoStripe(){
    //    visualController.OnNoStripe();
    //}







}

