using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


[RequireComponent(typeof(LevelPageVisuals))]
public class LevelPage : Page
{


   public new LevelPageVisualController VisualController => GetVisualController<LevelPageVisualController>();



   public override void InitVisuals()
   {
      visualController = new LevelPageVisualController();
      visualController.Init(this);
   }

   private void Update()
   {
      visualController.Update();
   }

    private void OnDestroy()
    {
      visualController.Unsubscribe();
    }

   
}