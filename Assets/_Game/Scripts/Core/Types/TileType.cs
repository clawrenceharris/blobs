namespace Blobs.Core
{
    public enum TileType
    {
        None,
        Grave

    }

    public static class TileTypeExtensions
    {
        public static bool IsGrave(this TileType type)
        {
            return type == TileType.Grave;
        }
        public static bool IsTraversable(this TileType type)
        {
            return type == TileType.Grave;
        }
    }
}