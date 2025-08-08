using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
public class LevelMenu : MonoBehaviour
{

    [SerializeField] private bool deletePrefs;
    
    public void OpenLevel()
    {
    
        SceneManager.LoadScene("Level");
    }

    public void BackToMainMenu()
    {
        SceneManager.LoadScene("Main Menu");
    }
    
    


}
