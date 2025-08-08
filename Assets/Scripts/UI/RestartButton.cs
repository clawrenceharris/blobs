using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RestartButton : MonoBehaviour
{

    private Button button;
    private LevelManager levelManager;
    private void Awake()
    {
        button = GetComponent<Button>();
        levelManager = FindFirstObjectByType<LevelManager>();
    }

    private void Start()
    {
        button.onClick.AddListener(OnClick);
    }


    private void OnClick()
    {
        levelManager.Restart();
    }
    

}
