using System;
using System.Collections.Generic;
using Blobs.Application;
using Blobs.Content;
using Blobs.Core;
using Blobs.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Blobs.Tests.EditMode
{
    public sealed class BoardSurfaceTests
    {
        private GameObject _root;

        [TestCase(false)]
        [TestCase(true)]
        public void MissingSurfaceUsesAlternatingSpritesAndPreservesCutouts(bool hasSurfacePresenter)
        {
            _root = new GameObject("Fallback Surface Test");
            if (hasSurfacePresenter) _root.AddComponent<BoardSurfacePresenter>();
            var presenter = _root.AddComponent<BoardPresenter>();
            var texture = new Texture2D(8, 4);
            var sprite = Sprite.Create(texture, new Rect(0, 0, 8, 4), Vector2.zero, 4f);
            try
            {
                var serialized = new SerializedObject(presenter);
                serialized.FindProperty("cellSprite").objectReferenceValue = sprite;
                serialized.FindProperty("origin").vector2Value = new Vector2(3f, 5f);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                var snapshot = new GameSessionSnapshot("fallback", new BoardState(2, 2,
                    Array.Empty<BlobState>(), Array.Empty<TileState>(),
                    new[] { new GridPosition(1, 1) }), 0, false);

                presenter.Rebuild(snapshot);
                Assert.That(presenter.VisibleSurfaceCellCount, Is.EqualTo(3));
                SpriteRenderer[] cells = _root.GetComponentsInChildren<SpriteRenderer>();
                Assert.That(cells.Length, Is.EqualTo(3));
                Assert.That(cells[0].bounds.center.x, Is.EqualTo(3f).Within(0.001f));
                Assert.That(cells[0].bounds.center.y, Is.EqualTo(5f).Within(0.001f));
                Assert.That(cells[0].bounds.size.x, Is.EqualTo(presenter.CellSize).Within(0.001f));
                Assert.That(cells[0].bounds.size.y, Is.EqualTo(presenter.CellSize).Within(0.001f));
                Assert.That(cells[1].color.r, Is.GreaterThan(cells[0].color.r));
                Assert.That(cells[1].color, Is.EqualTo(cells[2].color));
                foreach (var cell in cells)
                    Assert.That(cell.color.a, Is.EqualTo(0.15f).Within(0.001f));

                presenter.Rebuild(snapshot);
                Assert.That(_root.GetComponentsInChildren<SpriteRenderer>().Length, Is.EqualTo(3));
                presenter.Clear();
                Assert.That(presenter.VisibleSurfaceCellCount, Is.Zero);
                Assert.That(_root.GetComponentsInChildren<SpriteRenderer>(), Is.Empty);
            }
            finally
            {
                presenter.Clear();
                Object.DestroyImmediate(sprite);
                Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void MissingCellSpriteLogsAndSkipsSurfaceWithoutAddingRequiredSurfacePresenter()
        {
            _root = new GameObject("Missing Cell Sprite Test");
            var presenter = _root.AddComponent<BoardPresenter>();
            Assert.That(_root.GetComponent<BoardSurfacePresenter>(), Is.Null);
            LogAssert.Expect(LogType.Log,
                "BoardPresenter: no board surface presenter/view or Cell Sprite assigned; skipping the board surface.");
            presenter.Rebuild(EmptySnapshot());
            presenter.Rebuild(EmptySnapshot());
            Assert.That(presenter.VisibleSurfaceCellCount, Is.Zero);
            LogAssert.NoUnexpectedReceived();
        }

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
                "Assets/_Game/Production/Resources/BoardSurfaceSpriteSet.asset");

            Assert.That(spriteSet, Is.Not.Null);
            Assert.That(spriteSet.IsConfigured, Is.True);
            Assert.That(spriteSet.LogicalCellSize, Is.EqualTo(256));
            Assert.That(spriteSet.ComponentSize, Is.EqualTo(384));
            Assert.That(spriteSet.SeamOverlap, Is.EqualTo(2));
            Assert.That(spriteSet.FillA.texture.isReadable, Is.True);
            Assert.That(spriteSet.FillB.texture.isReadable, Is.True);
            Assert.That(spriteSet.FillA.texture.width, Is.EqualTo(384));
            Assert.That(spriteSet.FillA.texture.height, Is.EqualTo(384));
            Assert.That(spriteSet.FillA.textureRect.width, Is.EqualTo(384));
            Assert.That(spriteSet.FillA.textureRect.height, Is.EqualTo(384));
            Assert.That(spriteSet.CellInset, Is.Not.Null);
            Assert.That(spriteSet.CellInset.texture.isReadable, Is.True);
            Assert.That(spriteSet.CellInset.texture.width, Is.EqualTo(256));
            Assert.That(spriteSet.CellInset.texture.height, Is.EqualTo(256));

            Color32[] fill = spriteSet.FillA.texture.GetPixels32();
            int width = spriteSet.FillA.texture.width;
            Assert.That(fill[0].a, Is.EqualTo(0));
            Assert.That(fill[(width / 2) * width + width / 2].a, Is.EqualTo(255));

            Color32[] alternate = spriteSet.FillB.texture.GetPixels32();
            Assert.That(
                alternate[(width / 2) * width + width / 2],
                Is.Not.EqualTo(fill[(width / 2) * width + width / 2]));
        }

        [Test]
        public void ComposedCellBleedsOpaqueCoverageTowardPresentNeighbor()
        {
            BoardSurfaceSpriteSet spriteSet = AssetDatabase.LoadAssetAtPath<BoardSurfaceSpriteSet>(
                "Assets/_Game/Production/Resources/BoardSurfaceSpriteSet.asset");
            using var composer = new BoardSurfaceSpriteComposer(spriteSet);

            Sprite sprite = composer.GetOrCreate(BoardSurfaceNeighborMask.East);
            Color32[] pixels = Readback(sprite.texture);
            int size = spriteSet.ComponentSize;
            int boundaryX = spriteSet.ComponentPadding + spriteSet.LogicalCellSize;
            int centerY = spriteSet.ComponentPadding + spriteSet.LogicalCellSize / 2;

            Assert.That(pixels[centerY * size + boundaryX].a, Is.EqualTo(255));
            Assert.That(pixels[centerY * size + boundaryX + 1].a, Is.EqualTo(255));
        }

        [Test]
        public void SeamBleedDoesNotExtendPerimeterArtworkPastAConcaveTangent()
        {
            BoardSurfaceSpriteSet spriteSet = AssetDatabase.LoadAssetAtPath<BoardSurfaceSpriteSet>(
                "Assets/_Game/Production/Resources/BoardSurfaceSpriteSet.asset");
            using var composer = new BoardSurfaceSpriteComposer(spriteSet);

            Sprite sprite = composer.GetOrCreate(
                BoardSurfaceNeighborMask.North | BoardSurfaceNeighborMask.South);
            Color32[] pixels = Readback(sprite.texture);
            int size = spriteSet.ComponentSize;
            int min = spriteSet.ComponentPadding;
            int center = min + spriteSet.LogicalCellSize / 2;

            Assert.That(
                pixels[(min - 1) * size + min],
                Is.EqualTo(pixels[center * size + center]),
                "Only the flat fill may bleed through an internal seam; a west-edge " +
                "highlight here creates the protruding line at a concave join.");
        }

        [Test]
        public void ConcaveFillIsClippedToItsSharedRadiusSquare()
        {
            BoardSurfaceSpriteSet spriteSet = AssetDatabase.LoadAssetAtPath<BoardSurfaceSpriteSet>(
                "Assets/_Game/Production/Resources/BoardSurfaceSpriteSet.asset");
            using var composer = new BoardSurfaceSpriteComposer(spriteSet);

            Sprite sprite = composer.GetOrCreate(
                BoardSurfaceNeighborMask.North | BoardSurfaceNeighborMask.West);
            Color32[] pixels = Readback(sprite.texture);
            int size = spriteSet.ComponentSize;
            int imageX = spriteSet.ComponentPadding - spriteSet.CornerRadius - 8;
            int imageY = spriteSet.ComponentPadding - 1;
            int textureY = size - 1 - imageY;

            Assert.That(
                pixels[textureY * size + imageX].a,
                Is.EqualTo(0),
                "The concave fillet must not reach the component boundary and expose a hard cutoff.");
        }

        [Test]
        public void SouthEdgeHasAStrongDirectionalLowerLip()
        {
            BoardSurfaceSpriteSet spriteSet = AssetDatabase.LoadAssetAtPath<BoardSurfaceSpriteSet>(
                "Assets/_Game/Production/Resources/BoardSurfaceSpriteSet.asset");
            using var composer = new BoardSurfaceSpriteComposer(spriteSet);

            Sprite sprite = composer.GetOrCreate(BoardSurfaceNeighborMask.None);
            Color32[] pixels = Readback(sprite.texture);
            int size = spriteSet.ComponentSize;
            int imageX = spriteSet.ComponentPadding + spriteSet.LogicalCellSize / 2;
            int imageY = spriteSet.ComponentPadding + spriteSet.LogicalCellSize + 6;
            int textureY = size - 1 - imageY;

            Assert.That(
                pixels[textureY * size + imageX].a,
                Is.GreaterThanOrEqualTo(230),
                "The warm south-facing underside should remain visibly thick and opaque.");

            int lowerImageY = spriteSet.ComponentPadding + spriteSet.LogicalCellSize + 22;
            int lowerTextureY = size - 1 - lowerImageY;
            Assert.That(
                pixels[lowerTextureY * size + imageX].a,
                Is.GreaterThanOrEqualTo(220),
                "The south lip should retain substantial depth below the top surface.");

            Color32 surface = pixels[
                (size - 1 - (spriteSet.ComponentPadding + spriteSet.LogicalCellSize / 2))
                * size + imageX];
            Color32 front = pixels[lowerTextureY * size + imageX];
            int surfaceValue = surface.r + surface.g + surface.b;
            int frontValue = front.r + front.g + front.b;
            Assert.That(
                surfaceValue - frontValue,
                Is.GreaterThanOrEqualTo(75),
                "The front face must read as a separate, darker plane from the cream top.");
        }

        [Test]
        public void CellInsetHasTransparentCornersSubtleEdgeAndClearCenter()
        {
            BoardSurfaceSpriteSet spriteSet = AssetDatabase.LoadAssetAtPath<BoardSurfaceSpriteSet>(
                "Assets/_Game/Production/Resources/BoardSurfaceSpriteSet.asset");

            Color32[] pixels = spriteSet.CellInset.texture.GetPixels32();
            int size = spriteSet.LogicalCellSize;
            Assert.That(pixels[0].a, Is.EqualTo(0));
            Assert.That(pixels[size - 1].a, Is.EqualTo(0));
            Assert.That(pixels[(size - 1) * size].a, Is.EqualTo(0));
            Assert.That(pixels[size * size - 1].a, Is.EqualTo(0));

            int edgeAlpha = pixels[(size - 1 - 8) * size + size / 2].a;
            int centerAlpha = pixels[(size / 2) * size + size / 2].a;
            Assert.That(edgeAlpha, Is.InRange(24, 160));
            Assert.That(centerAlpha, Is.LessThanOrEqualTo(8));
        }

        [Test]
        public void ComposerUsesDistinctFillForCheckerParity()
        {
            BoardSurfaceSpriteSet spriteSet = AssetDatabase.LoadAssetAtPath<BoardSurfaceSpriteSet>(
                "Assets/_Game/Production/Resources/BoardSurfaceSpriteSet.asset");
            using var composer = new BoardSurfaceSpriteComposer(spriteSet);

            Sprite fillA = composer.GetOrCreate(BoardSurfaceNeighborMask.None, false);
            Sprite fillB = composer.GetOrCreate(BoardSurfaceNeighborMask.None, true);
            int center = spriteSet.ComponentSize / 2;

            Color32[] pixelsA = Readback(fillA.texture);
            Color32[] pixelsB = Readback(fillB.texture);
            Assert.That(
                pixelsA[center * spriteSet.ComponentSize + center],
                Is.Not.EqualTo(pixelsB[center * spriteSet.ComponentSize + center]));
        }


        [Test]
        public void SurfaceViewAcceptsAnArbitraryOccupiedCellShape()
        {
            _root = new GameObject("Irregular Board Surface Test");
            BoardSurfaceView view = _root.AddComponent<BoardSurfaceView>();
            TestPresentationComposition.ConfigureSurface(view);
            var occupied = new HashSet<GridPosition>
            {
                new GridPosition(0, 0),
                new GridPosition(1, 0),
                new GridPosition(0, 1)
            };

            view.Rebuild(occupied, 1f, Vector2.zero);

            Assert.That(view.VisibleCellCount, Is.EqualTo(3));
            Assert.That(
                view.GetComponentsInChildren<SpriteRenderer>().Length,
                Is.EqualTo(1));
            Assert.That(
                view.TryGetMask(new GridPosition(0, 0), out BoardSurfaceNeighborMask mask),
                Is.True);
            Assert.That(mask.HasFlag(BoardSurfaceNeighborMask.NorthEast), Is.False);
        }

        private const int BakeResolution = 128;

        [Test]
        public void BakerFramesASingleCellOnEverySideInsteadOfShrinkingIt()
        {
            var occupied = new HashSet<GridPosition> { new GridPosition(0, 0) };
            using BoardSurfaceBaker.Bake bake = BoardSurfaceBaker.BakeOccupied(
                occupied, BoardSurfaceBaker.DefaultStyle(BakeResolution));
            int pad = bake.Pad;
            int cell = bake.PixelsPerCell;
            int margin = bake.FrameMargin;
            int mid = pad + cell / 2;

            Assert.That(bake.Get(mid, mid).a, Is.EqualTo(255));
            Assert.That(bake.Get(1, 1).a, Is.LessThan(30));

            // The cream frame is a dilation of occupancy, so it exists outside the
            // logical cell on north, east and west, not only under the south lip.
            Assert.That(bake.Get(mid, pad + cell + margin / 2).a, Is.EqualTo(255), "north frame");
            Assert.That(bake.Get(pad - margin / 2, mid).a, Is.EqualTo(255), "west frame");
            Assert.That(bake.Get(pad + cell + margin / 2, mid).a, Is.EqualTo(255), "east frame");
            Assert.That(
                bake.Get(mid, pad + cell + margin + 6).a,
                Is.LessThan(200),
                "The frame must stop a fixed distance outside the cell.");
        }

        [Test]
        public void BakerGivesExposedAndInteriorCellsTheSamePad()
        {
            // A plus: the centre cell is enclosed, the arms are exposed on three sides.
            var occupied = new HashSet<GridPosition>
            {
                new GridPosition(1, 1),
                new GridPosition(0, 1),
                new GridPosition(2, 1),
                new GridPosition(1, 0),
                new GridPosition(1, 2)
            };
            using BoardSurfaceBaker.Bake bake = BoardSurfaceBaker.BakeOccupied(
                occupied, BoardSurfaceBaker.DefaultStyle(BakeResolution));

            int interior = PadEdgeOffset(bake, new GridPosition(1, 1), -1, 0);
            foreach (GridPosition exposed in occupied)
            {
                Assert.That(
                    PadEdgeOffset(bake, exposed, -1, 0),
                    Is.EqualTo(interior).Within(1),
                    $"west pad edge of {exposed} drifted from the interior cell");
                Assert.That(
                    PadEdgeOffset(bake, exposed, 0, -1),
                    Is.EqualTo(interior).Within(1),
                    $"south pad edge of {exposed} drifted from the interior cell");
            }
        }

        [Test]
        public void BakerKeepsEveryCellCentreInsideItsOwnPad()
        {
            var occupied = new HashSet<GridPosition>
            {
                new GridPosition(0, 0),
                new GridPosition(1, 0),
                new GridPosition(0, 1)
            };
            using BoardSurfaceBaker.Bake bake = BoardSurfaceBaker.BakeOccupied(
                occupied, BoardSurfaceBaker.DefaultStyle(BakeResolution));
            int cell = bake.PixelsPerCell;

            // Pieces are placed on the logical lattice, so the lattice centre has to
            // land on flat pad floor rather than on a groove or the frame.
            foreach (GridPosition position in occupied)
            {
                Color32 centre = CellPixel(bake, position, 0, 0);
                Assert.That(centre.a, Is.EqualTo(255));
                Assert.That(
                    CellPixel(bake, position, cell / 4, cell / 4),
                    Is.EqualTo(centre),
                    $"pad floor of {position} is not flat around its centre");
            }
        }

        [Test]
        public void BakerSharesASeamAndKeepsTheCheckerSubtle()
        {
            var occupied = new HashSet<GridPosition>
            {
                new GridPosition(0, 0),
                new GridPosition(1, 0)
            };
            using BoardSurfaceBaker.Bake bake = BoardSurfaceBaker.BakeOccupied(
                occupied, BoardSurfaceBaker.DefaultStyle(BakeResolution));
            int pad = bake.Pad;
            int cell = bake.PixelsPerCell;
            Color32 seam = bake.Get(pad + cell, pad + cell / 2);
            Color32 left = CellPixel(bake, new GridPosition(0, 0), 0, 0);
            Color32 right = CellPixel(bake, new GridPosition(1, 0), 0, 0);

            Assert.That(seam.a, Is.EqualTo(255), "adjacent cells must not open a gap");
            Assert.That(left, Is.Not.EqualTo(right));
            int delta = Mathf.Max(
                Mathf.Abs(left.r - right.r),
                Mathf.Abs(left.g - right.g),
                Mathf.Abs(left.b - right.b));
            Assert.That(delta, Is.GreaterThan(0));
            Assert.That(delta, Is.LessThanOrEqualTo(16));
        }

        [Test]
        public void BakerAntiAliasesTheSilhouetteWithPartialCoverage()
        {
            var occupied = new HashSet<GridPosition> { new GridPosition(0, 0) };
            using BoardSurfaceBaker.Bake bake = BoardSurfaceBaker.BakeOccupied(
                occupied, BoardSurfaceBaker.DefaultStyle(BakeResolution));
            int cell = bake.PixelsPerCell;
            int radius = Mathf.RoundToInt(51f * cell / 256f);
            int y = bake.Pad + cell + bake.FrameMargin - radius / 2;

            // Walking in along a rounded corner must cross a partially covered pixel;
            // a binary hull mask can only ever produce 0 or 255 here.
            bool sawCoverage = false;
            for (int x = 0; x < bake.Width && !sawCoverage; x++)
                sawCoverage = bake.Get(x, y).a is > 40 and < 215;

            Assert.That(sawCoverage, Is.True, "the silhouette is not coverage anti-aliased");
        }

        [Test]
        public void BakerKeepsTheSouthLipAThinStepUnderTheSlab()
        {
            var occupied = new HashSet<GridPosition> { new GridPosition(0, 0) };
            using BoardSurfaceBaker.Bake bake = BoardSurfaceBaker.BakeOccupied(
                occupied, BoardSurfaceBaker.DefaultStyle(BakeResolution));
            int pad = bake.Pad;
            int cell = bake.PixelsPerCell;
            int margin = bake.FrameMargin;
            int lip = bake.LipOffset;
            int mid = pad + cell / 2;
            int wallTop = pad - margin;

            Assert.That(lip, Is.LessThanOrEqualTo(cell * 3 / 20), "the lip must read as a step, not a crust");
            Color32 wall = bake.Get(mid, wallTop - lip / 2);
            Color32 face = bake.Get(mid, mid);
            Assert.That(wall.a, Is.GreaterThanOrEqualTo(200));
            Assert.That(
                face.r + face.g + face.b - (wall.r + wall.g + wall.b),
                Is.GreaterThanOrEqualTo(40),
                "the wall must read as a separate, darker plane from the cream top");
            Assert.That(
                bake.Get(mid, wallTop - lip - 4).a,
                Is.LessThan(200),
                "nothing opaque may hang below the wall");
            Assert.That(
                bake.Get(mid, Mathf.Max(0, wallTop - lip - 12)).a,
                Is.GreaterThanOrEqualTo(50),
                "The contact shadow under the slab should remain visible.");
        }

        [Test]
        public void BakerLipTintShiftsTheUndersideTowardThePalette()
        {
            var occupied = new HashSet<GridPosition> { new GridPosition(0, 0) };
            BoardSurfaceBaker.Style purple = BoardSurfaceBaker.DefaultStyle(BakeResolution);
            BoardSurfaceBaker.Style yellow = purple;
            yellow.LipTint = new Color(1f, 0.82f, 0.2f, 1f);
            yellow.LipTintStrength = 0.85f;

            using BoardSurfaceBaker.Bake purpleBake =
                BoardSurfaceBaker.BakeOccupied(occupied, purple);
            using BoardSurfaceBaker.Bake yellowBake =
                BoardSurfaceBaker.BakeOccupied(occupied, yellow);
            int x = purpleBake.Pad + purpleBake.PixelsPerCell / 2;
            int y = purpleBake.Pad - purpleBake.FrameMargin - purpleBake.LipOffset / 2;
            Color32 purpleLip = purpleBake.Get(x, y);
            Color32 yellowLip = yellowBake.Get(x, y);

            Assert.That(purpleLip.a, Is.GreaterThanOrEqualTo(200));
            Assert.That(yellowLip.a, Is.GreaterThanOrEqualTo(200));
            Assert.That(yellowLip.b, Is.LessThan(purpleLip.b));
            Assert.That(yellowLip.r, Is.GreaterThan(purpleLip.r));
        }

        [Test]
        public void BakerFramesAHoleAndFilletsAConcaveCorner()
        {
            var donut = new HashSet<GridPosition>();
            for (int x = 0; x < 3; x++)
                for (int y = 0; y < 3; y++)
                {
                    if (x == 1 && y == 1)
                        continue;
                    donut.Add(new GridPosition(x, y));
                }

            using BoardSurfaceBaker.Bake bake = BoardSurfaceBaker.BakeOccupied(
                donut, BoardSurfaceBaker.DefaultStyle(BakeResolution));
            int pad = bake.Pad;
            int cell = bake.PixelsPerCell;
            int margin = bake.FrameMargin;

            Assert.That(
                bake.Get(pad + cell + cell / 2, pad + cell + cell / 2).a,
                Is.LessThan(40),
                "the hole must stay open");
            Assert.That(
                bake.Get(pad + cell / 2, pad + cell + cell / 2).a,
                Is.EqualTo(255));
            Assert.That(
                bake.Get(pad + cell + margin / 2, pad + cell + cell / 2).a,
                Is.EqualTo(255),
                "the frame wraps the hole too, so the hole is smaller than a cell");

            var ell = new HashSet<GridPosition>
            {
                new GridPosition(0, 1),
                new GridPosition(1, 0),
                new GridPosition(1, 1)
            };
            using BoardSurfaceBaker.Bake bakedEll = BoardSurfaceBaker.BakeOccupied(
                ell, BoardSurfaceBaker.DefaultStyle(BakeResolution));
            Assert.That(
                bakedEll.Get(bakedEll.Pad + bakedEll.PixelsPerCell - 4,
                    bakedEll.Pad + bakedEll.PixelsPerCell - 4).a,
                Is.EqualTo(255),
                "the concave join must be filleted, not notched open");
        }

        [Test]
        public void DefaultPaletteMatchesTheBakerArtDirection()
        {
            BoardSurfacePaletteAsset palette =
                AssetDatabase.LoadAssetAtPath<BoardSurfacePaletteAsset>(
                    "Assets/_Game/Production/Content/Presentation/BoardSurfacePalette_Default.asset");
            BoardSurfaceBaker.Style plain = BoardSurfaceBaker.DefaultStyle(BakeResolution);
            BoardSurfaceBaker.Style tinted = BoardSurfaceBaker.WithPalette(plain, palette);

            // The preview PNG is baked from the Python defaults, which mirror
            // DefaultStyle. If the shipped palette drifts, the preview stops being a
            // truthful reference for what ships.
            Assert.That(tinted.Fill, Is.EqualTo(plain.Fill));
            Assert.That(tinted.FillB, Is.EqualTo(plain.FillB));
            Assert.That(tinted.Ambient, Is.EqualTo(plain.Ambient));
            Assert.That(tinted.Highlight, Is.EqualTo(plain.Highlight));
            Assert.That(tinted.Shadow, Is.EqualTo(plain.Shadow));
            Assert.That(tinted.LipShade, Is.EqualTo(plain.LipShade));
            Assert.That(tinted.LipTint, Is.EqualTo(plain.LipTint));
            Assert.That(tinted.LipTintStrength, Is.EqualTo(plain.LipTintStrength).Within(0.001f));
        }

        private static Color32 CellPixel(
            BoardSurfaceBaker.Bake bake, GridPosition position, int offsetX, int offsetY)
        {
            int cell = bake.PixelsPerCell;
            int x = bake.Pad + (position.X - bake.MinX) * cell + cell / 2 + offsetX;
            int y = bake.Pad + (position.Y - bake.MinY) * cell + cell / 2 + offsetY;
            return bake.Get(x, y);
        }

        /// <summary>
        /// Distance from a cell's centre to the darkest pixel of its pad groove along
        /// the given direction. Uniform pads put this at the same offset for every cell.
        /// </summary>
        private static int PadEdgeOffset(
            BoardSurfaceBaker.Bake bake, GridPosition position, int stepX, int stepY)
        {
            int cell = bake.PixelsPerCell;
            int limit = cell / 2 + bake.FrameMargin;
            int darkest = 0;
            int lowest = int.MaxValue;
            for (int step = cell / 4; step <= limit; step++)
            {
                Color32 pixel = CellPixel(bake, position, stepX * step, stepY * step);
                if (pixel.a < 255)
                    break;
                int value = pixel.r + pixel.g + pixel.b;
                if (value < lowest)
                {
                    lowest = value;
                    darkest = step;
                }
            }

            return darkest;
        }





        [Test]
        public void MissingSurfaceLayoutFallsBackToFullBoardDimensions()
        {
            _root = new GameObject("Rectangular Surface Fallback Test");
            BoardPresenter presenter = TestPresentationComposition.AddBoardPresenter(_root);
            presenter.Initialize(
                new FakeGameplayState(EmptySnapshot()),
                null,
                new EmptyBlobViewFactory());

            Assert.That(presenter.VisibleSurfaceCellCount, Is.EqualTo(6));
        }

        [Test]
        public void PresenterBuildsSurfaceFromDimensionsAndCutoutsWithoutLogicalTiles()
        {
            _root = new GameObject("Cutout Surface Test");
            BoardPresenter presenter = TestPresentationComposition.AddBoardPresenter(_root);
            var snapshot = new GameSessionSnapshot("cutout", new BoardState(2, 2,
                Array.Empty<BlobState>(), Array.Empty<TileState>(),
                new[] { new GridPosition(1, 1) }), 0, false);
            presenter.Initialize(new FakeGameplayState(snapshot), null, new EmptyBlobViewFactory());
            Assert.That(presenter.VisibleSurfaceCellCount, Is.EqualTo(3));
            var surface = _root.GetComponentInChildren<BoardSurfaceView>();
            Assert.That(surface.TryGetMask(new GridPosition(1, 1), out _), Is.False);
            Assert.That(surface.TryGetMask(new GridPosition(0, 0), out var mask), Is.True);
            Assert.That(mask.HasFlag(BoardSurfaceNeighborMask.North), Is.True);
            Assert.That(mask.HasFlag(BoardSurfaceNeighborMask.East), Is.True);
            Assert.That(mask.HasFlag(BoardSurfaceNeighborMask.NorthEast), Is.False);
        }

        private static GameSessionSnapshot EmptySnapshot()
        {
            return new GameSessionSnapshot(
                "board-surface-test",
                new BoardState(2, 3, new HashSet<BlobState>(), new HashSet<TileState>()),
                0,
                false);
        }


        private static Color32[] Readback(Texture texture)
        {
            RenderTexture previous = RenderTexture.active;
            RenderTexture target = RenderTexture.GetTemporary(
                texture.width,
                texture.height,
                0,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.sRGB);
            var readable = new Texture2D(
                texture.width,
                texture.height,
                TextureFormat.RGBA32,
                false,
                false);
            try
            {
                Graphics.Blit(texture, target);
                RenderTexture.active = target;
                readable.ReadPixels(
                    new Rect(0f, 0f, target.width, target.height),
                    0,
                    0,
                    false);
                readable.Apply(false, false);
                return readable.GetPixels32();
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
                Object.DestroyImmediate(readable);
            }
        }

        private sealed class EmptyBlobViewFactory : IBlobViewFactory
        {
            public BlobView Create(
                BlobState blob,
                Transform parent,
                float cellSize,
                Vector2 origin)
            {
                return null;
            }
        }

        private sealed class FakeGameplayState : IGameplayState
        {
            private readonly GameSessionSnapshot _snapshot;
            public event Action<BlobSelectionResult> BlobSelected;

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

            public event Action<UndoResult> UndoResolved
            {
                add { }
                remove { }
            }

            public bool CanUndo => false;

            public GameSessionSnapshot CreateSnapshot()
            {
                return _snapshot;
            }
        }
    }
}
