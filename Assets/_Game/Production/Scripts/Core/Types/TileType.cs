namespace Blobs.Core
{
    public enum TileType
    {
        Sigil

    }

    public static class TileTypeExtensions
    {
        public static bool IsSigil(this TileType type)
        {
            return type == TileType.Sigil;
        }
        public static bool IsTraversable(this TileType type)
        {
            return type == TileType.Sigil;
        }
    }
}