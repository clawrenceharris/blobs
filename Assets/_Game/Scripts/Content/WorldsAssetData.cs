using System;

namespace Blobs.Content
{
    [Serializable]
    public sealed class WorldAssetData
    {
        public string Name;

    }
    [Serializable]
    public sealed class WorldsAssetData
    {
        public WorldAssetData[] worlds;
    }
}