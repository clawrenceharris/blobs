using System;
using System.Collections.Generic;
using Blobs.Core;

namespace Blobs.Presentation
{
    /// <summary>
    /// Occupancy bits surrounding one logical board cell.
    /// </summary>
    [Flags]
    public enum BoardSurfaceNeighborMask : byte
    {
        None = 0,
        North = 1 << 0,
        East = 1 << 1,
        South = 1 << 2,
        West = 1 << 3,
        NorthWest = 1 << 4,
        NorthEast = 1 << 5,
        SouthWest = 1 << 6,
        SouthEast = 1 << 7
    }

    /// <summary>
    /// Atlas components selected for one occupied cell.
    /// Fill is always present and is therefore not represented by a flag.
    /// </summary>
    [Flags]
    public enum BoardSurfacePieces : ushort
    {
        None = 0,
        EdgeNorth = 1 << 0,
        EdgeEast = 1 << 1,
        EdgeSouth = 1 << 2,
        EdgeWest = 1 << 3,
        ConvexNorthWest = 1 << 4,
        ConvexNorthEast = 1 << 5,
        ConvexSouthWest = 1 << 6,
        ConvexSouthEast = 1 << 7,
        ConcaveNorthWest = 1 << 8,
        ConcaveNorthEast = 1 << 9,
        ConcaveSouthWest = 1 << 10,
        ConcaveSouthEast = 1 << 11
    }

    /// <summary>
    /// Converts visual tile occupancy into deterministic edge and corner selections.
    /// This class reads positions only and never mutates board state.
    /// </summary>
    public static class BoardSurfaceNeighborMaskResolver
    {
        public static BoardSurfaceNeighborMask Resolve(
            ISet<GridPosition> occupied,
            GridPosition cell)
        {
            if (occupied == null)
                throw new ArgumentNullException(nameof(occupied));

            BoardSurfaceNeighborMask mask = BoardSurfaceNeighborMask.None;
            SetIfOccupied(occupied, cell, 0, 1, BoardSurfaceNeighborMask.North, ref mask);
            SetIfOccupied(occupied, cell, 1, 0, BoardSurfaceNeighborMask.East, ref mask);
            SetIfOccupied(occupied, cell, 0, -1, BoardSurfaceNeighborMask.South, ref mask);
            SetIfOccupied(occupied, cell, -1, 0, BoardSurfaceNeighborMask.West, ref mask);
            SetIfOccupied(occupied, cell, -1, 1, BoardSurfaceNeighborMask.NorthWest, ref mask);
            SetIfOccupied(occupied, cell, 1, 1, BoardSurfaceNeighborMask.NorthEast, ref mask);
            SetIfOccupied(occupied, cell, -1, -1, BoardSurfaceNeighborMask.SouthWest, ref mask);
            SetIfOccupied(occupied, cell, 1, -1, BoardSurfaceNeighborMask.SouthEast, ref mask);
            return mask;
        }

        public static BoardSurfacePieces SelectPieces(BoardSurfaceNeighborMask mask)
        {
            bool north = Has(mask, BoardSurfaceNeighborMask.North);
            bool east = Has(mask, BoardSurfaceNeighborMask.East);
            bool south = Has(mask, BoardSurfaceNeighborMask.South);
            bool west = Has(mask, BoardSurfaceNeighborMask.West);

            BoardSurfacePieces pieces = BoardSurfacePieces.None;
            if (!north) pieces |= BoardSurfacePieces.EdgeNorth;
            if (!east) pieces |= BoardSurfacePieces.EdgeEast;
            if (!south) pieces |= BoardSurfacePieces.EdgeSouth;
            if (!west) pieces |= BoardSurfacePieces.EdgeWest;

            pieces |= SelectCorner(
                north,
                west,
                Has(mask, BoardSurfaceNeighborMask.NorthWest),
                BoardSurfacePieces.ConvexNorthWest,
                BoardSurfacePieces.ConcaveNorthWest);
            pieces |= SelectCorner(
                north,
                east,
                Has(mask, BoardSurfaceNeighborMask.NorthEast),
                BoardSurfacePieces.ConvexNorthEast,
                BoardSurfacePieces.ConcaveNorthEast);
            pieces |= SelectCorner(
                south,
                west,
                Has(mask, BoardSurfaceNeighborMask.SouthWest),
                BoardSurfacePieces.ConvexSouthWest,
                BoardSurfacePieces.ConcaveSouthWest);
            pieces |= SelectCorner(
                south,
                east,
                Has(mask, BoardSurfaceNeighborMask.SouthEast),
                BoardSurfacePieces.ConvexSouthEast,
                BoardSurfacePieces.ConcaveSouthEast);
            return pieces;
        }

        private static BoardSurfacePieces SelectCorner(
            bool firstCardinal,
            bool secondCardinal,
            bool diagonal,
            BoardSurfacePieces convex,
            BoardSurfacePieces concave)
        {
            if (!firstCardinal && !secondCardinal)
                return convex;
            if (firstCardinal && secondCardinal && !diagonal)
                return concave;
            return BoardSurfacePieces.None;
        }

        private static bool Has(
            BoardSurfaceNeighborMask mask,
            BoardSurfaceNeighborMask value)
        {
            return (mask & value) != 0;
        }

        private static void SetIfOccupied(
            ISet<GridPosition> occupied,
            GridPosition cell,
            int offsetX,
            int offsetY,
            BoardSurfaceNeighborMask value,
            ref BoardSurfaceNeighborMask mask)
        {
            if (occupied.Contains(new GridPosition(cell.X + offsetX, cell.Y + offsetY)))
                mask |= value;
        }
    }
}
