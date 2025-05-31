
using System;
using TMPro;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UI;


public class LevelButton : MonoBehaviour
{
    private int levelNum = 1;
    private MenuController menuController;
    private LevelManager levelManager;
    [SerializeField] private Image[] stars;
    private Button button;
    private int levelNumber;
    private TextMeshProUGUI buttonText;


    private void Awake()
    {
        levelManager = FindFirstObjectByType<LevelManager>();
        buttonText = GetComponentInChildren<TextMeshProUGUI>();
    }
   
    
    private void OnEnable()
    {
        ActivateStars();

    }


    

    private void SaveSelectedLevel()
    {
        PlayerData data = GameManager.PlayerData;
        data.selectedLevel = levelNumber;
        GameManager.PlayerData = data;
    }

   
    private void ActivateStars()
    {
        
        PlayerData data = GameManager.PlayerData;

        if (data.levelStars.ContainsKey(levelNumber))
        {
            for (int i = 0; i < data.levelStars[levelNumber]; i++)
            {
                stars[i].enabled = true;
            }
        }
    }
    private void Start()
    {
        
    }
    public void Init(int levelNum)
    {
        this.levelNum = levelNum;
        buttonText.text = levelNum.ToString();
    }
    
  

    
    private void SetUp()
    {

        SaveManager saveManager = SaveManager.Instance;
        if (saveManager.PlayerData == null)
        {
            return;
        }
        // var progressData  =  progress.GetProgressData(levelNum);
        // for (int i = 0; i < progressData.stars; i++)
        // {
        //     stars[i].SetActive(true);
        // }
    }
    public void StartLevel()
    {
        MenuController.Instance.ReplacePage(PageType.Level);
        levelManager.StartLevel(levelNum);


    }
    
    

    


}