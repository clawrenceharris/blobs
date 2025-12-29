
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.VisualScripting;

public class GameManager : MonoBehaviour
{
   
    [SerializeField] private World[] worlds;
    public static GameManager Instance { get; private set; }
    public static PlayerData PlayerData
    {
        get
        {
            return SaveManager.Instance.LoadData();
        }
        set
        {
            SaveManager.Instance.SaveData(value);
        }
    }
    public World[] Worlds {
        get
        {
            return worlds;
        }
    }

    [SerializeField]private int worldIndex;
    public static int TotalAmountOfLevels { get; private set; }

    public World CurrentWorld
    {
        get
        {
            return worlds[worldIndex];
        }
    }

    private void Awake()
    {
       
        if (Instance == null)
        {
           
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
           
            Destroy(gameObject);
        }
        SetTotalAmountOfLevels();
    }

    private void SetTotalAmountOfLevels()
    {
        // int total = 0;
        // foreach (World world in worlds)
        // {
        //     total += world.levels.Length;
        // }
        // TotalAmountOfLevels = total;
    }
    
   
    


    

    
}
