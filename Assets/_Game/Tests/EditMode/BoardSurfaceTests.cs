using System.Collections.Generic;
using Blobs.Core;
using Blobs.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Blobs.Tests.EditMode
{
    public sealed class BoardSurfaceTests
    {
        private GameObject _root;

        [TearDown]
        public void TearDown()
        {
            if (_root != null)
                Object.DestroyImmediate(_root);
        }

        [Test]
        public void IsolatedCellSelectsFourEdgesAndFourConvexCorners()
        {
            var occupied = new HashSet<GridPosition> { new GridPosition(0, 0) };

            BoardSurfaceNeighborMask mask = BoardSurfaceNeighborMaskResolver.Resolve(
                occupied,
                new GridPosition(0, 0));
            BoardSurfacePieces pieces = BoardSurfaceNeighborMaskResolver.SelectPieces(mask);

            Assert.That(mask, Is.EqualTo(BoardSurfaceNeighborMask.None));
            Assert.That(pieces, Is.EqualTo(
                BoardSurfacePieces.EdgeNorth |
                BoardSurfacePieces.EdgeEast |
                BoardSurfacePieces.EdgeSouth |
                BoardSurfacePieces.EdgeWest |
                BoardSurfacePieces.ConvexNorthWest |
                BoardSurfacePieces.ConvexNorthEast |
                BoardSurfacePieces.ConvexSouthWest |
                BoardSurfacePieces.ConvexSouthEast));
        }

        [Test]
        public void PresentCardinalsAndMissingDiagonalSelectConcaveCorner()
        {
            var center = new GridPosition(0, 0);
            var occupied = new HashSet<GridPosition>
            {
                center,
                new GridPosition(0, 1),
                new GridPosition(-1, 0)
            };

            BoardSurfaceNeighborMask mask = BoardSurfaceNeighborMaskResolver.Resolve(occupied, center);
            BoardSurfacePieces pieces = BoardSurfaceNeighborMaskResolver.SelectPieces(mask);

            Assert.That(pieces.HasFlag(BoardSurfacePieces.ConcaveNorthWest), Is.True);
            Assert.That(pieces.HasFlag(BoardSurfacePieces.ConvexNorthWest), Is.False);
            Assert.That(pieces.HasFlag(BoardSurfacePieces.EdgeNorth), Is.False);
            Assert.That(pieces.HasFlag(BoardSurfacePieces.EdgeWest), Is.False);
        }

        [Test]
        public void PresentDiagonalDoesNotSelectConcaveCorner()
        {
            var center = new GridPosition(0, 0);
            var occupied = new HashSet<GridPosition>
            {
                center,
                new GridPosition(0, 1),
                new GridPosition(-1, 0),
                new GridPosition(-1, 1)
            };

            BoardSurfacePieces pieces = BoardSurfaceNeighborMaskResolver.SelectPieces(
                BoardSurfaceNeighborMaskResolver.Resolve(occupied, center));

            Assert.That(pieces.HasFlag(BoardSurfacePieces.ConcaveNorthWest), Is.False);
            Assert.That(pieces.HasFlag(BoardSurfacePieces.ConvexNorthWest), Is.False);
        }

        [Test]
        public void GeneratedSpriteSetHasReadableTrueAlphaComponents()
        {
            BoardSurfaceSpriteSet spriteSet = AssetDatabase.LoadAssetAtPath<BoardSurfaceSpriteSet>(
                "Assets/_Game/Content/Board/BoardSurfaceSpriteSet.asset");

            Assert.That(spriteSet, Is.Not.Null);
            Assert.That(spriteSet.IsConfigured, Is.True);
            Assert.That(spriteSet.LogicalCellSize, Is.EqualTo(256));
            Assert.That(spriteSet.ComponentSize, Is.EqualTo(384));
            Assert.That(spriteSet.SeamOverlap, Is.EqualTo(2));
            Assert.That(spriteSet.Fill.texture.isReadable, Is.True);
            Assert.That(spriteSet.Fill.texture.width, Is.EqualTo(384));
            Assert.That(spriteSet.Fill.texture.height, Is.EqualTo(384));
            Assert.That(spriteSet.Fill.textureRect.width, Is.EqualTo(384));
            Assert.That(spriteSet.Fill.textureRect.height, Is.EqualTo(384));

            Color32[] fill = spriteSet.Fill.texture.GetPixels32();
            int width = spriteSet.Fill.texture.width;
            Assert.That(fill[0].a, Is.EqualTo(0));
            Assert.That(fill[(width / 2) * width + width / 2].a, Is.EqualTo(255));
        }

        [Test]
        public void ComposedCellBleedsOpaqueCoverageTowardPresentNeighbor()
        {
            BoardSurfaceSpriteSet spriteSet = AssetDatabase.LoadAssetAtPath<BoardSurfaceSpriteSet>(
                "Assets/_Game/Content/Board/BoardSurfaceSpriteSet.asset");
            using var composer = new BoardSurfaceSpriteComposer(spriteSet);

            Sprite sprite = composer.GetOrCreate(BoardSurfaceNeighborMask.East);
            Color32[] pixels = sprite.texture.GetPixels32();
            int size = spriteSet.ComponentSize;
            int boundaryX = spriteSet.ComponentPadding + spriteSet.LogicalCellSize;
            int centerY = spriteSet.ComponentPadding + spriteSet.LogicalCellSize / 2;

            Assert.That(pixels[centerY * size + boundaryX].a, Is.EqualTo(255));
            Assert.That(pixels[centerY * size + boundaryX + 1].a, Is.EqualTo(255));
        }

        [Test]
        public void SurfaceViewBuildsOneVisualPerLogicalTile()
        {
            _root = new GameObject("Board Surface Test");
            BoardSurfaceView view = _root.AddComponent<BoardSurfaceView>();
            var tiles = new List<TileState>
            {
                Tile("origin", 0, 0),
                Tile("east", 1, 0),
                Tile("north", 0, 1)
            };

            view.Rebuild(tiles, 1.25f, Vector2.zero);

            Assert.That(view.VisibleCellCount, Is.EqualTo(3));
            Assert.That(view.TryGetMask(new GridPosition(0, 0), out BoardSurfaceNeighborMask mask), Is.True);
            Assert.That(mask.HasFlag(BoardSurfaceNeighborMask.North), Is.True);
            Assert.That(mask.HasFlag(BoardSurfaceNeighborMask.East), Is.True);
            Assert.That(mask.HasFlag(BoardSurfaceNeighborMask.NorthEast), Is.False);
            Assert.That(_root.GetComponentsInChildren<SpriteRenderer>().Length, Is.EqualTo(3));
        }

        [Test]
        public void SurfaceViewAcceptsAnArbitraryOccupiedCellShape()
        {
            _root = new GameObject("Irregular Board Surface Test");
            BoardSurfaceView view = _root.AddComponent<BoardSurfaceView>();
            var occupied = new HashSet<GridPosition>
            {
                new GridPosition(0, 0),
                new GridPosition(1, 0),
                new GridPosition(0, 1)
            };

            view.Rebuild(occupied, 1f, Vector2.zero);

            Assert.That(view.VisibleCellCount, Is.EqualTo(3));
            Assert.That(
                view.TryGetMask(new GridPosition(0, 0), out BoardSurfaceNeighborMask mask),
                Is.True);
            Assert.That(mask.HasFlag(BoardSurfaceNeighborMask.NorthEast), Is.False);
        }

        private static TileState Tile(string id, int x, int y)
        {
            return new TileState(id, new GridPosition(x, y), TileType.Normal);
        }
    }
}
