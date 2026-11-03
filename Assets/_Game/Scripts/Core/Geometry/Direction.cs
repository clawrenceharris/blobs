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

        /// <summary>Unit grid offset for one step in this direction.</summary>
        public static GridPosition ToOffset(this Direction direction)
        {
            switch (direction)
            {
                case Direction.North: return new GridPosition(0, 1);
                case Direction.East: return new GridPosition(1, 0);
                case Direction.South: return new GridPosition(0, -1);
                case Direction.West: return new GridPosition(-1, 0);
                default: return GridPosition.Zero;
            }
        }

        /// <summary>
        /// Cardinal direction of a non-zero axis-aligned delta. Defaults to <see cref="Direction.South"/>
        /// when the delta is zero or diagonal.
        /// </summary>
        public static Direction FromAxisAligned(GridPosition delta)
        {
            if (delta.X == 0 && delta.Y == 0)
                return Direction.South;
            if (System.Math.Abs(delta.X) >= System.Math.Abs(delta.Y))
                return delta.X > 0 ? Direction.East : Direction.West;
            return delta.Y > 0 ? Direction.North : Direction.South;
        }
    }
}