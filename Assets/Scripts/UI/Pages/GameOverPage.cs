using TMPro;
using UnityEngine;


[RequireComponent(typeof(GameOverPageVisuals))]

public class GameOverPage : Page
{
   [SerializeField] private TextMeshProUGUI scoreText;
   
   public override void InitVisuals()
   {
      visualController = new GameOverPageVisualController();
      visualController.Init(this);
   }


}