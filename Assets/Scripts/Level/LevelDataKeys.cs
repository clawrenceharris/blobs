



using System;
using System.Collections.Generic;
using static Type;
public static class LevelDataKeys
{
    public const string Type = "t";
    public const string Color = "c";
    public const string Size = "s";
    public const string X = "x";

    public const string Y = "y";

    public const string Width = "width";
    public const string LevelNum = "levelNum";

    public const string Height = "height";

    public const string Blobs = "blobs";

    public const string Tiles = "tiles";


    public static class  DotColors{
        public const string Red = "r";
        public const string Yellow = "y";
        public const string Green = "g";
        public const string Blue = "b";
        public const string Purple = "p";  
        public const string Pink = "pk";  
        public const string LightBlue = "lb";  

        public const string Blank = "x";  

        
    }
    public static class Types{
        public const string NormalBlob = "nb";
        public const string FlagBlob = "fb";
        public const string NormalTile = "nt";
        public const string SpikeTile = "st";

        public const string TargetTile = "tt";
        public const string SwitchBlob = "sb";
        public const string TrailBlob = "tb";


               

        



         public static readonly Dictionary<string, BlobType> dotTypeMap = new Dictionary<string, BlobType>()
        {
            { NormalBlob, BlobType.Normal },
            { TrailBlob, BlobType.Trail },
            { SwitchBlob, BlobType.Switch },
            { FlagBlob, BlobType.Flag  },
           
        };

        public static readonly Dictionary<string, TileType> tileTypeMap = new Dictionary<string, TileType>()
        {
            { NormalTile, TileType.Normal },
            { TargetTile, TileType.Target },
            { SpikeTile, TileType.Spike },
        };

        public static BlobType GetDotTypeFromKey(string key)
        {
            if (dotTypeMap.TryGetValue(key, out BlobType value))
            {
                return value;
            }
            throw new ArgumentException($"Dot type key '{key}' not found.");
        }

        public static TileType GetTileTypeFromKey(string key)
        {
            if (tileTypeMap.TryGetValue(key, out TileType value))
            {
                return value;
            }
            throw new ArgumentException($"Tile type key '{key}' not found.");
        }
    }
}

