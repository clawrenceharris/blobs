



using System;
using System.Collections.Generic;

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
    public const string LaserLinks = "laserLinks";

    public static object TutorialSteps  = "tutorialSteps";

    public static class Types
    {
        public const string NormalBlob = "nb";
        public const string FlagBlob = "fb";
        public const string SwitchBlob = "sb";
        public const string TrailBlob = "tb";
        public const string BombBlob = "bb";
        public const string GhostBlob = "gb";

        public const string EnemyBlob = "eb";
        public const string RockBlob = "rb";

        public const string NormalTile = "nt";
        public const string SpikeTile = "st";

        public const string TargetTile = "tt";
      
        public const string SigilTile = "sgt";


        public const string LaserTile = "lt";




        public static readonly Dictionary<string, BlobType> blobTypeMap = new()
        {
            { NormalBlob, BlobType.Normal },
            { TrailBlob, BlobType.Trail },
            { SwitchBlob, BlobType.Switch },
            { FlagBlob, BlobType.Flag  },
            { BombBlob, BlobType.Bomb },
            { GhostBlob, BlobType.Ghost },
            { EnemyBlob, BlobType.Enemy },
            { RockBlob, BlobType.Rock },

        };

        public static readonly Dictionary<string, TileType> tileTypeMap = new()
        {
            { NormalTile, TileType.Normal },
            { TargetTile, TileType.Target },
            { SpikeTile, TileType.Spike },
            { SigilTile, TileType.Sigil },
            { LaserTile, TileType.Laser },

        };

        public static BlobType GetBlobTypeFromKey(string key)
        {
            if (blobTypeMap.TryGetValue(key, out BlobType value))
            {
                return value;
            }
            throw new ArgumentException($"Blob type key '{key}' not found.");
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
    public static class  BlobColors{
        public const string Red = "r";
        public const string Yellow = "y";
        public const string Green = "g";
        public const string Blue = "b";
        public const string Purple = "p";  
        public const string Pink = "pk";  
        public const string LightBlue = "lb";  
        public const string TrailColor = "tc";

        public const string Blank = "x";  
 public static readonly Dictionary<string, BlobColor> blobColorMap = new Dictionary<string, BlobColor>()
        {
            { Red, BlobColor.Red },
            { Yellow, BlobColor.Yellow },
            { Blue, BlobColor.Blue },
            { LightBlue, BlobColor.LightBlue },
            { Green, BlobColor.Green },
            { Purple, BlobColor.Purple },
            { Blank, BlobColor.Blank },
            { Pink, BlobColor.Pink },


        };
        public static BlobColor GetBlobColorFromKey(string key)
        {
            if (blobColorMap.TryGetValue(key, out BlobColor value))
            {
                return value;
            }
            throw new ArgumentException($"Blob color key '{key}' not found.");
        }

        
    }
    public static class BlobSizes
    {
        public const string Big = "b";
        public const string Normal = "n";
        public const string Small = "s";

        public static readonly Dictionary<string, BlobSize> blobSizeMap = new()
        {
            { Big, BlobSize.Big },
            { Normal, BlobSize.Normal },
            { Small, BlobSize.Small },

        };
        public static BlobSize GetBlobSizeFromKey(string key)
        {
            if (blobSizeMap.TryGetValue(key, out BlobSize value))
            {
                return value;
            }
            throw new ArgumentException($"Blob size key '{key}' not found.");
        }

    }
    
}

