using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

using Newtonsoft.Json;

public class PlayerData
{

    public Dictionary<int, int> levelStars = new();
    public Dictionary<int, int> levelPoints = new();
    public List<int> completedLevels = new();
    public int selectedLevel = 0;
    
    public PlayerData(List<int> completedLevels)
    {
        this.completedLevels = completedLevels;
    }
}

public class SaveManager : MonoBehaviour
{
    private string savePath;
    public static SaveManager Instance { get; private set; }
    [SerializeField] private bool deletePlayerData;
    public PlayerData PlayerData { get; private set; }
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
        savePath = Path.Combine(Application.persistentDataPath, "playerData.json");

        if(LoadData() == null || deletePlayerData)
            //Save new data with level 0 complete
            SaveData(new PlayerData(new List<int>{0}));

        


    }

    public void SaveData(PlayerData data)
    {

        string json = JsonConvert.SerializeObject(data);

        File.WriteAllText(savePath, json);
        Debug.Log("File saved to: " +savePath);
    }

    public PlayerData LoadData()
    {

        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            PlayerData data = JsonConvert.DeserializeObject<PlayerData>(json);
            PlayerData = data;
            return data;
        }
        else
        {
            Debug.LogWarning("data file not found!");
            return null;
        }
    }
}
