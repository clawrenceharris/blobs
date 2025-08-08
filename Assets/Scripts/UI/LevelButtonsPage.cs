using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelButtonsPage : MonoBehaviour
{

    [SerializeField] private GameObject completedLevelButton;
    [SerializeField] private GameObject currentLevelButton;
    [SerializeField] private GameObject lockedLevelButton;
    [SerializeField] private int pageNumber;
    [SerializeField] private RectTransform[] pages;

    void Start()
    {
        CreateLevelButtons();
    }

    private void CreateLevelButtons()
    {

        PlayerData data = SaveManager.Instance.LoadData();
        int start = pageNumber * (GameManager.TotalAmountOfLevels / 3) - (GameManager.TotalAmountOfLevels / 3);
        for (int i = start; i < (GameManager.TotalAmountOfLevels / 3) * pageNumber; i++)
        {
            {
                if (data.completedLevels.Count >0&& data.completedLevels[^1] == i)
                {
                    LevelButton levelButton = Instantiate(currentLevelButton).GetComponent<LevelButton>();
                    levelButton.transform.SetParent(transform);
                    levelButton.transform.localScale = Vector3.one;
                    levelButton.Init(i + 1);
                }

                else if (data.completedLevels.Contains(i + 1))
                {
                    LevelButton levelButton = Instantiate(completedLevelButton).GetComponent<LevelButton>();
                    levelButton.transform.SetParent(transform);
                    levelButton.transform.localScale = Vector3.one;
                    levelButton.Init(i + 1);
                }

                else
                {
                    GameObject lockedButton = Instantiate(lockedLevelButton);
                    lockedButton.transform.SetParent(transform);
                    lockedButton.transform.localScale = Vector3.one;
                }









            }
        }


        
    }
}
