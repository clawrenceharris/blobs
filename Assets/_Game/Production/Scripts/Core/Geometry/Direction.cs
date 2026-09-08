namespace Blobs.Core
{
    public enum Direction
    {
        North,
        East,
        South,
        West
    }

    public static class DirectionExtensions
    {
        public static Direction Left(this Direction direction) =>
            (Direction)(((int)direction + 3) % 4);

        public static Direction Right(this Direction direction) =>
            (Direction)(((int)direction + 1) % 4);

        public static Direction Back(this Direction direction) =>
            (Direction)(((int)direction + 2) % 4);
    }
}