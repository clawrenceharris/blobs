using System;

namespace Blobs.Core
{
    public interface IMove
    {
        GridPosition To { get; }
        GridPosition From { get; }
    }

    public readonly struct Move : IEquatable<Move>
    {
        public GridPosition To { get; }
        public GridPosition From { get; }

        public static Move None() => new(GridPosition.Zero, GridPosition.Zero);

        public Move(GridPosition from, GridPosition to)
        {
            To = to;
            From = from;
        }

        public bool Equals(Move other)
        {
            return To == other.To && From == other.From;
        }
    }
}
