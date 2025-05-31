using UnityEngine;

public class LevelSelectPage : Page
{
    [SerializeField]private int totalLevels;
    [SerializeField] private RectTransform gridTransform;
    [SerializeField] private LevelButton levelButton;
    
    private void Start()
    {
        for (int i = 0; i < totalLevels; i++)
        {
            LevelButton button = Instantiate(levelButton);
            button.transform.SetParent(gridTransform);
            button.Init(i + 1);

        }
    }
}