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
    }    public static BlobColor FromJsonColor(string color)
    {
        return color switch
        {
            "purple" => BlobColor.Purple,
            "pink" => BlobColor.Pink,
            "orange" => BlobColor.Orange,
            "red" => BlobColor.Red,
            "light blue" => BlobColor.LightBlue,
            "dark blue" => BlobColor.Blue,
            "green" => BlobColor.Green,
            "yellow" => BlobColor.Yellow,
            _ => BlobColor.None,
        };
    }


    public static Type.TileType FromJsonTileType(string type)
    {
        return type switch
        {
            LevelDataKeys.Types.NormalTile => Type.TileType.Normal,
             LevelDataKeys.Types.SpikeTile => Type.TileType.Spike,
             LevelDataKeys.Types.TargetTile => Type.TileType.Target,
            
            _ => Type.TileType.None,
        };
    }

    public static BlobsGameObject FromJsonBlobType(string type)
    {
        return type switch
        {
            LevelDataKeys.Types.NormalBlob => GameAssets.Instance.NormalBlob,
            LevelDataKeys.Types.TrailBlob => GameAssets.Instance.TrailBlob,

            LevelDataKeys.Types.FlagBlob =>GameAssets.Instance.FlagBlob,
            LevelDataKeys.Types.SwitchBlob => GameAssets.Instance.SwitchBlob,
            LevelDataKeys.Types.TargetTile =>GameAssets.Instance.TargetTile,
            LevelDataKeys.Types.NormalTile => GameAssets.Instance.NormalTile,
            LevelDataKeys.Types.SpikeTile => GameAssets.Instance.SpikeTile,


            _ => throw new ArgumentException("no object matches the given type: '" + type + "'"),
        };
    }
}



[Serializable]
public class BlobsData
{
    public int Col;
    public int Row;
    public string Type;
    
    public enum Property{
        Color,
        Type,
        Size,

        Position
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

