using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using Newtonsoft.Json;
public class LevelLoader
{

    public static LevelData Level;
     public static LevelData LoadLevelData(TextAsset textAsset){

        var settings = new JsonSerializerSettings
        {
            Converters = { new LevelDataConverter() }
        };
        Level = JsonConvert.DeserializeObject<LevelData>(textAsset.text, settings);
        return Level;
    }

    
    public static LevelData LoadLevelData(int levelNum)
    {
        string path = Application.dataPath + "/Levels/level_" + levelNum + ".json";
        string json = File.ReadAllText(path);

        var settings = new JsonSerializerSettings
        {
            Converters = { new LevelDataConverter() }
        };
        LevelData level = JsonConvert.DeserializeObject<LevelData>(json, settings);
        return level;
    }  

   

}



[Serializable]
public class JSONBlobObject
{
    public int X;
    public int Y;
    public string Type;
    
    public enum Property{
        Color,
        Type,
        Size,
        TrailColor,
        Position,
        Index
    }
    // Dictionary to hold dynamic properties
    private readonly Dictionary<Property, object> properties = new();

    public T GetProperty<T>(Property key)
    {
        if (properties.ContainsKey(key))
        {
            return (T)properties[key];
        }
        return default;
    }

    public void SetProperty<T>(Property key, T value)
    {
        if (properties.ContainsKey(key))
        {
            properties[key] = value;
        }
        else
        {
            properties.Add(key, value);
        }
    }
}

