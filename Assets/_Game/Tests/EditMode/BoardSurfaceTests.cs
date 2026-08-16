using System;
using System.Collections.Generic;
using Blobs.Application;
using Blobs.Content;
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

        [Test]
        public void Level03UsesItsAuthoredSevenCellSurfaceLayout()
        {
            LevelDefinitionAsset level = AssetDatabase.LoadAssetAtPath<LevelDefinitionAsset>(
                "Assets/_Game/Content/Levels/SO/Level_03.asset");

            Assert.That(level, Is.Not.Null);
            Assert.That(level.BoardSurfaceLayout, Is.Not.Null);
            Assert.That(
                level.BoardSurfaceLayout.TryValidate(level.Width, level.Height, out string error),
                Is.True,
                error);
            Assert.That(level.BoardSurfaceLayout.OccupiedCells, Is.EquivalentTo(new[]
            {
                new Vector2Int(0, 0),
                new Vector2Int(1, 0),
                new Vector2Int(2, 0),
                new Vector2Int(3, 0),
                new Vector2Int(3, 1),
                new Vector2Int(3, 2),
                new Vector2Int(3, 3)
            }));

            _root = new GameObject("Level 3 Surface Layout Test");
            BoardPresenter presenter = _root.AddComponent<BoardPresenter>();
            presenter.Initialize(
                new FakeGameplayState(EmptySnapshot()),
                null,
                surfaceWidth: level.Width,
                surfaceHeight: level.Height,
                surfaceLayout: level.BoardSurfaceLayout);

            Assert.That(presenter.VisibleSurfaceCellCount, Is.EqualTo(7));
            BoardSurfaceView surface = _root.GetComponentInChildren<BoardSurfaceView>(true);
            Assert.That(
                surface.TryGetMask(new GridPosition(3, 0), out BoardSurfaceNeighborMask mask),
                Is.True);
            Assert.That(mask.HasFlag(BoardSurfaceNeighborMask.North), Is.True);
            Assert.That(mask.HasFlag(BoardSurfaceNeighborMask.West), Is.True);
            Assert.That(mask.HasFlag(BoardSurfaceNeighborMask.NorthWest), Is.False);
        }

        [Test]
        public void DefaultBoardSurfacePaletteExistsForFutureIntegration()
        {
            BoardSurfacePaletteAsset palette =
                AssetDatabase.LoadAssetAtPath<BoardSurfacePaletteAsset>(
                    "Assets/_Game/Content/Board/Palettes/BoardSurfacePalette_Default.asset");

            Assert.That(palette, Is.Not.Null);
            Assert.That(palette.Surface.a, Is.EqualTo(1f));
            Assert.That(palette.Highlight.r, Is.GreaterThan(palette.Surface.r));
            Assert.That(palette.Thickness.r, Is.LessThan(palette.Surface.r));
        }

        [Test]
        public void SurfaceLayoutRejectsDuplicateAndOutOfBoundsCells()
        {
            BoardSurfaceLayoutAsset layout =
                ScriptableObject.CreateInstance<BoardSurfaceLayoutAsset>();
            try
            {
                var serialized = new SerializedObject(layout);
                SerializedProperty cells = serialized.FindProperty("occupiedCells");
                cells.arraySize = 2;
                cells.GetArrayElementAtIndex(0).vector2IntValue = new Vector2Int(1, 1);
                cells.GetArrayElementAtIndex(1).vector2IntValue = new Vector2Int(1, 1);
                serialized.ApplyModifiedPropertiesWithoutUndo();

                Assert.That(layout.TryValidate(4, 4, out string duplicateError), Is.False);
                StringAssert.Contains("more than once", duplicateError);

                cells.GetArrayElementAtIndex(1).vector2IntValue = new Vector2Int(4, 1);
                serialized.ApplyModifiedPropertiesWithoutUndo();

                Assert.That(layout.TryValidate(4, 4, out string boundsError), Is.False);
                StringAssert.Contains("outside board dimensions", boundsError);
            }
            finally
            {
                Object.DestroyImmediate(layout);
            }
        }

        [Test]
        public void MissingSurfaceLayoutFallsBackToFullBoardDimensions()
        {
            _root = new GameObject("Rectangular Surface Fallback Test");
            BoardPresenter presenter = _root.AddComponent<BoardPresenter>();
            presenter.Initialize(
                new FakeGameplayState(EmptySnapshot()),
                null,
                surfaceWidth: 2,
                surfaceHeight: 3);

            Assert.That(presenter.VisibleSurfaceCellCount, Is.EqualTo(6));
        }

        private static GameSessionSnapshot EmptySnapshot()
        {
            return new GameSessionSnapshot(
                "board-surface-test",
                new BlobState[0],
                new TileState[0],
                0,
                false);
        }

        private static TileState Tile(string id, int x, int y)
        {
            return new TileState(id, new GridPosition(x, y), TileType.Normal);
        }

        private sealed class FakeGameplayState : IGameplayState
        {
            private readonly GameSessionSnapshot _snapshot;

            public FakeGameplayState(GameSessionSnapshot snapshot)
            {
                _snapshot = snapshot;
            }

            public event Action<GameSessionSnapshot> SnapshotChanged
            {
                add { }
                remove { }
            }

            public event Action<MoveResult> MoveResolved
            {
                add { }
                remove { }
            }

            public event Action<GameSessionSnapshot> StateRestored
            {
                add { }
                remove { }
            }

            public GameSessionSnapshot CreateSnapshot()
            {
                return _snapshot;
            }
        }
    }
}
