using System;
using System.Collections.Generic;
using UnityEngine;

namespace Blobs.Editor
{
    /// <summary>
    /// Defines which fields apply per blob type for dynamic editor UI and placement defaults.
    /// </summary>
    public struct BlobTypeSchema
    {
        public BlobType Type;
        public bool UsesColor;
        public bool UsesSize;
        public bool UsesTrailColor;
        public BlobColor DefaultColor;
        public BlobSize DefaultSize;
        public string ShortLabel; // single char for palette button

        public static IReadOnlyList<BlobTypeSchema> All => _all;

        private static readonly BlobTypeSchema[] _all =
        {
            new BlobTypeSchema
            {
                Type = BlobType.Normal,
                UsesColor = true,
                UsesSize = true,
                UsesTrailColor = false,
                DefaultColor = BlobColor.Pink,
                DefaultSize = BlobSize.Normal,
                ShortLabel = "N"
            },
            new BlobTypeSchema
            {
                Type = BlobType.Trail,
                UsesColor = true,
                UsesSize = true,
                UsesTrailColor = true,
                DefaultColor = BlobColor.Pink,
                DefaultSize = BlobSize.Normal,
                ShortLabel = "T"
            },
            new BlobTypeSchema
            {
                Type = BlobType.Ghost,
                UsesColor = false,
                UsesSize = false,
                UsesTrailColor = false,
                DefaultColor = BlobColor.Blank,
                DefaultSize = BlobSize.None,
                ShortLabel = "G"
            },
            new BlobTypeSchema
            {
                Type = BlobType.Target,
                UsesColor = true,
                UsesSize = false,
                UsesTrailColor = false,
                DefaultColor = BlobColor.Pink,
                DefaultSize = BlobSize.Normal,
                ShortLabel = "F"
            },
            new BlobTypeSchema
            {
                Type = BlobType.Bomb,
                UsesColor = false,
                UsesSize = false,
                UsesTrailColor = false,
                DefaultColor = BlobColor.Blank,
                DefaultSize = BlobSize.Normal,
                ShortLabel = "B"
            },
            new BlobTypeSchema
            {
                Type = BlobType.Switch,
                UsesColor = true,
                UsesSize = false,
                UsesTrailColor = false,
                DefaultColor = BlobColor.Pink,
                DefaultSize = BlobSize.Normal,
                ShortLabel = "S"
            },
            new BlobTypeSchema
            {
                Type = BlobType.Enemy,
                UsesColor = true,
                UsesSize = false,
                UsesTrailColor = false,
                DefaultColor = BlobColor.Pink,
                DefaultSize = BlobSize.Normal,
                ShortLabel = "E"
            },
            new BlobTypeSchema
            {
                Type = BlobType.Rock,
                UsesColor = false,
                UsesSize = false,
                UsesTrailColor = false,
                DefaultColor = BlobColor.Blank,
                DefaultSize = BlobSize.Normal,
                ShortLabel = "R"
            }
        };

        public static BlobTypeSchema Get(BlobType type)
        {
            foreach (var s in _all)
                if (s.Type == type) return s;
            return _all[0];
        }

        public static int IndexOf(BlobType type)
        {
            for (int i = 0; i < _all.Length; i++)
                if (_all[i].Type == type) return i;
            return 0;
        }
    }

    /// <summary>
    /// Defines which fields apply per tile type for dynamic editor UI.
    /// </summary>
    public struct TileTypeSchema
    {
        public TileType Type;
        public bool UsesLaserId;
        public bool UsesLaserColor;

        public static IReadOnlyList<TileTypeSchema> All => _all;

        private static readonly TileTypeSchema[] _all =
        {
            new TileTypeSchema { Type = TileType.Normal, UsesLaserId = false, UsesLaserColor = false },
            new TileTypeSchema { Type = TileType.Spike, UsesLaserId = false, UsesLaserColor = false },
            new TileTypeSchema { Type = TileType.Laser, UsesLaserId = true, UsesLaserColor = true },
            new TileTypeSchema { Type = TileType.Sigil, UsesLaserId = false, UsesLaserColor = false },
            new TileTypeSchema { Type = TileType.Sticky, UsesLaserId = false, UsesLaserColor = false },
            new TileTypeSchema { Type = TileType.Ice, UsesLaserId = false, UsesLaserColor = false },
            new TileTypeSchema { Type = TileType.Target, UsesLaserId = false, UsesLaserColor = false }
        };

        public static TileTypeSchema Get(TileType type)
        {
            int i = (int)type;
            if (i >= 0 && i < _all.Length) return _all[i];
            return _all[0];
        }
    }
}
