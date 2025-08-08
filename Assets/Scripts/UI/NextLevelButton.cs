using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class NextLevelButton : MonoBehaviour
{
    private Button button;
    private LevelManager levelManager;
    void Awake()
    {
        button = GetComponent<Button>();
        levelManager = FindFirstObjectByType<LevelManager>();
    }
    private void Start()
    {
        button.onClick.AddListener(OnClick);
        if (!CanClick())
        {
            button.interactable = false;
        }
    }

    private void OnClick()
    {
        levelManager.StartNextLevel();
        
    }

    private bool CanClick()
    {
        PlayerData data = SaveManager.Instance.LoadData();
        return GameManager.Instance.CurrentWorld.levels.Length < data.selectedLevel + 1 && data.completedLevels[^1] > data.selectedLevel +1;
        
    }
    

   
}
