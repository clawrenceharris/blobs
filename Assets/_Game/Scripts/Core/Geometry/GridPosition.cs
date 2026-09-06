using System;
using System.Numerics;

namespace Blobs.Core
{
    /// <summary>
    /// Integer grid coordinate used by Core. World-space conversion belongs in Presentation.
    /// </summary>
    public readonly struct GridPosition : IEquatable<GridPosition>
    {
        /// <summary>
        /// Creates a logical grid position.
        /// </summary>
        public GridPosition(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; }
        public int Y { get; }

        /// <summary>
        /// Returns true when the positions share a row or column.
        /// </summary>
        public bool IsAlignedWith(GridPosition other)
        {
            return X == other.X || Y == other.Y;
        }

        public GridPosition Offset(Direction direction)
        {
            switch (direction)
            {
                case Direction.North: return new GridPosition(X, Y + 1);
                case Direction.East: return new GridPosition(X + 1, Y);
                case Direction.South: return new GridPosition(X, Y - 1);
                case Direction.West: return new GridPosition(X - 1, Y);
                default: throw new ArgumentOutOfRangeException(nameof(direction));
            }
        }

        public bool Equals(GridPosition other)
        {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            return obj is GridPosition other && Equals(other);
        }



        public static GridPosition Zero = new GridPosition(0, 0);

        public override string ToString() => $"({X},{Y})";

        public override int GetHashCode() => HashCode.Combine(X, Y);

        public static bool operator ==(GridPosition left, GridPosition right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GridPosition left, GridPosition right)
        {
            return !left.Equals(right);
        }

        public static GridPosition operator -(GridPosition left, GridPosition right)
        {
            return new GridPosition(left.X - right.X, left.Y - right.Y);
        }
        public static GridPosition operator +(GridPosition left, GridPosition right)
        {
            return new GridPosition(left.X + right.X, left.Y + right.Y);
        }

        // Normalizes the position to a unit step (cardinal direction only: up, down, left, right, or zero)
        // This will return (1,0), (-1,0), (0,1), (0,-1), or (0,0). Diagonal vectors become (0,0).
        public static GridPosition Normalize(GridPosition position)
        {
            int x = position.X;
            int y = position.Y;

            // Clamp to -1, 0, or 1 for each axis
            x = x > 0 ? 1 : (x < 0 ? -1 : 0);
            y = y > 0 ? 1 : (y < 0 ? -1 : 0);

            return new GridPosition(x, y);

        }
    }
}
