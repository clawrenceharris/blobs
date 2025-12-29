using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using Newtonsoft.Json;
public class LevelLoader
{

    public static List<LevelData> Levels = new();
     public static LevelData LoadLevelData(string json){
        var settings = new JsonSerializerSettings
        {
            Converters = { new LevelDataConverter() }
        };
        var level = JsonConvert.DeserializeObject<LevelData>(json, settings);
        return level;
    }
    public static BlobType FromJsonBlobType(string type)
    {
        return type switch
        {
            LevelDataKeys.Types.NormalBlob => BlobType.Normal,
            LevelDataKeys.Types.RockBlob => BlobType.Rock,
            LevelDataKeys.Types.BombBlob => BlobType.Bomb,
            LevelDataKeys.Types.GhostBlob => BlobType.Ghost,
            LevelDataKeys.Types.FlagBlob => BlobType.Flag,
            LevelDataKeys.Types.EnemyBlob => BlobType.Enemy,
            LevelDataKeys.Types.SwitchBlob => BlobType.Switch,
            LevelDataKeys.Types.TrailBlob => BlobType.Trail,
            _ => throw new ArgumentException(type + " is not a valid Json game object type"),
        };
    }
    public static string ToJsonBlobType(BlobType type)
    {
        return type switch
        {
            BlobType.Normal => LevelDataKeys.Types.NormalBlob,
            BlobType.Rock => LevelDataKeys.Types.RockBlob,
            BlobType.Bomb => LevelDataKeys.Types.BombBlob,
            BlobType.Ghost => LevelDataKeys.Types.GhostBlob,
            BlobType.Flag => LevelDataKeys.Types.FlagBlob,
            BlobType.Enemy => LevelDataKeys.Types.EnemyBlob,
            BlobType.Switch => LevelDataKeys.Types.SwitchBlob,
            BlobType.Trail => LevelDataKeys.Types.TrailBlob,
            _ => throw new ArgumentException(type + " is not a valid Json game object type"),
        };
    }
    public static TileType FromJsonTileType(string type)
    {
        return type switch
        {
            LevelDataKeys.Types.SigilTile => TileType.Sigil,
            LevelDataKeys.Types.LaserTile => TileType.Laser,
            LevelDataKeys.Types.SpikeTile => TileType.Spike,
            LevelDataKeys.Types.NormalTile => TileType.Normal,
            _ => throw new ArgumentException(type + " is not a valid JSON game object type"),
        };
    }
      public static string ToJsonTileType(TileType type)
    {
        return type switch
        {
            TileType.Sigil => LevelDataKeys.Types.SigilTile,
            TileType.Laser => LevelDataKeys.Types.LaserTile,
            TileType.Spike => LevelDataKeys.Types.SpikeTile,
            TileType.Normal => LevelDataKeys.Types.NormalTile,
            _ => throw new ArgumentException(type + " is not a valid JSON game object type"),
        };
    }
     
    public static void LoadAllLevels()
    {
        int levelNum = 1;
        
        while(true)
        {
            string path = Application.dataPath + "/Levels/level_" + levelNum + ".json";
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);


                LevelData level = LoadLevelData(json);
                Levels.Add(level);
                levelNum++;
            }
            else
            {
                break;
            }

            
        }
       
    }

    public static BlobColor FromJsonColor(string color)
    {
        return color switch
        {
            LevelDataKeys.BlobColors.Red => BlobColor.Red,
            LevelDataKeys.BlobColors.Blank => BlobColor.Blank,
            LevelDataKeys.BlobColors.LightBlue => BlobColor.LightBlue,
            LevelDataKeys.BlobColors.Blue => BlobColor.Blue,
            LevelDataKeys.BlobColors.Pink => BlobColor.Pink,
            LevelDataKeys.BlobColors.Green => BlobColor.Green,
            LevelDataKeys.BlobColors.Purple => BlobColor.Purple,
            LevelDataKeys.BlobColors.Yellow => BlobColor.Yellow,

            _ => throw new ArgumentException(color + " is not a valid JSON game object color"),
        };
    }
    public static string ToJsonColor(BlobColor color)
    {
        return color switch
        {
            BlobColor.Red => LevelDataKeys.BlobColors.Red,
            BlobColor.Blank => LevelDataKeys.BlobColors.Blank,
            BlobColor.LightBlue => LevelDataKeys.BlobColors.LightBlue,
            BlobColor.Blue =>  LevelDataKeys.BlobColors.Blue,
             BlobColor.Pink => LevelDataKeys.BlobColors.Pink,
            BlobColor.Green => LevelDataKeys.BlobColors.Green,
             BlobColor.Purple => LevelDataKeys.BlobColors.Purple,
             BlobColor.Yellow => LevelDataKeys.BlobColors.Yellow,

            _ => throw new ArgumentException(color + " is not a valid Blob color"),
        };
    }

    public static BlobSize FromJsonSize(string size)
    {
        return size switch
        {
            LevelDataKeys.BlobSizes.Big => BlobSize.Big,
            LevelDataKeys.BlobSizes.Normal => BlobSize.Normal,
            LevelDataKeys.BlobSizes.Small => BlobSize.Small,

            _ => throw new ArgumentException(size + " is not a valid JSON game object size"),
        };
    }
    public static string ToJsonSize(BlobSize size)
    {
        return size switch
        {
             BlobSize.Big => LevelDataKeys.BlobSizes.Big,
            BlobSize.Normal =>  LevelDataKeys.BlobSizes.Normal,
            BlobSize.Small =>  LevelDataKeys.BlobSizes.Small,

            _ => throw new ArgumentException(size + " is not a valid JSON game object size"),
        };
    }
}



[Serializable]
public class BlobData
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

