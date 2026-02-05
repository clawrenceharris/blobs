using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using Newtonsoft.Json;
public class LevelLoader
{

    public static List<LevelData> Levels = new();
     public static LevelData LoadLevelData(string json){
        // var settings = new JsonSerializerSettings
        // {
        //     Converters = { new LevelDataConverter() }
        // };
        var level = JsonConvert.DeserializeObject<LevelData>(json);
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
     
    /// <summary>
    /// Load levels from ScriptableObject assets in Resources/Levels first;
    /// if none found, fall back to JSON files under Assets/Levels.
    /// </summary>
    public static void LoadAllLevels()
    {
        Levels.Clear();
        LevelData[] assets = Resources.LoadAll<LevelData>("Levels");
        if (assets != null && assets.Length > 0)
        {
            System.Array.Sort(assets, (a, b) => a.LevelNumber.CompareTo(b.LevelNumber));
            Levels.AddRange(assets);
            return;
        }
        int levelNum = 1;
        while (true)
        {
            string path = Application.dataPath + "/Levels/level_" + levelNum + ".json";
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                LevelData level = LoadLevelData(json);
                if (level != null)
                    Levels.Add(level);
                levelNum++;
            }
            else
                break;
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

    /// <summary>
    /// Set a property by string key (e.g. from BlobSpawnData.Properties).
    /// Supports: color, c, size, s, trailColor, tc.
    /// </summary>
    public void SetProperty(string key, string value)
    {
        if (string.IsNullOrEmpty(key)) return;
        var k = key.Trim().ToLowerInvariant();
        if (k == "color" || k == "c") SetProperty(Property.Color, value);
        else if (k == "size" || k == "s") SetProperty(Property.Size, value);
        else if (k == "trailcolor" || k == "tc") SetProperty(Property.TrailColor, value);
    }
}

