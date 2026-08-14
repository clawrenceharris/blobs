using System;

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

        public bool Equals(GridPosition other)
        {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            return obj is GridPosition other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X * 397) ^ Y;
            }
        }

        public override string ToString()
        {
            return X + "," + Y;
        }

        public static bool operator ==(GridPosition left, GridPosition right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GridPosition left, GridPosition right)
        {
            return !left.Equals(right);
        }
    }
}
