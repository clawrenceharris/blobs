using System;
using Blobs.Core;

namespace Blobs.Content
{

    public sealed class WorldDefinition
    {


        public WorldDefinition(string id, string name, string description, int schemaVersion, LevelDefinition[] levels = null, BlobType? blobType = null, TileType? tileType = null, MechanicType? mechanicType = null)
        {
            WorldId = id;
            Name = name;
            Description = description;
            SchemaVersion = schemaVersion;
            Levels = levels ?? throw new ArgumentNullException(nameof(levels));
            BlobType = blobType;
            TileType = tileType;
            if (blobType == null && tileType == null)
            {
                throw new Exception("BlobType and TileType cannot both be null");
            }
        }

        public LevelDefinition[] Levels { get; set; }

        public string WorldId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int SchemaVersion { get; set; }
        public BlobType? BlobType { get; set; }
        public TileType? TileType { get; set; }


    }
}