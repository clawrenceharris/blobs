using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Blobs.Presentation
{
    /// <summary>
    /// Composes and caches one sprite for each encountered neighbor mask. Convex and
    /// concave components cut the base fill before their perimeter treatment is applied.
    /// </summary>
    public sealed class BoardSurfaceSpriteComposer : IDisposable
    {
        private static readonly BoardSurfacePieces[] EdgePieces =
        {
            BoardSurfacePieces.EdgeNorth,
            BoardSurfacePieces.EdgeEast,
            BoardSurfacePieces.EdgeSouth,
            BoardSurfacePieces.EdgeWest
        };

        private static readonly BoardSurfacePieces[] CornerPieces =
        {
            BoardSurfacePieces.ConvexNorthWest,
            BoardSurfacePieces.ConvexNorthEast,
            BoardSurfacePieces.ConvexSouthWest,
            BoardSurfacePieces.ConvexSouthEast,
            BoardSurfacePieces.ConcaveNorthWest,
            BoardSurfacePieces.ConcaveNorthEast,
            BoardSurfacePieces.ConcaveSouthWest,
            BoardSurfacePieces.ConcaveSouthEast
        };

        private readonly BoardSurfaceSpriteSet _spriteSet;
        private readonly Dictionary<BoardSurfaceNeighborMask, Sprite> _sprites =
            new Dictionary<BoardSurfaceNeighborMask, Sprite>();
        private readonly Dictionary<Sprite, Color32[]> _sourcePixels =
            new Dictionary<Sprite, Color32[]>();

        public BoardSurfaceSpriteComposer(BoardSurfaceSpriteSet spriteSet)
        {
            _spriteSet = spriteSet != null
                ? spriteSet
                : throw new ArgumentNullException(nameof(spriteSet));

            if (!_spriteSet.IsConfigured)
                throw new ArgumentException("Board surface sprite set is incomplete.", nameof(spriteSet));
        }

        public Sprite GetOrCreate(BoardSurfaceNeighborMask mask)
        {
            if (_sprites.TryGetValue(mask, out Sprite cached) && cached != null)
                return cached;

            int size = _spriteSet.ComponentSize;
            Color32[] output = CopyPixels(_spriteSet.Fill);
            BoardSurfacePieces selected = BoardSurfaceNeighborMaskResolver.SelectPieces(mask);

            foreach (BoardSurfacePieces edge in EdgePieces)
            {
                if (Has(selected, edge))
                    Composite(output, PixelsFor(_spriteSet.GetPiece(edge)));
            }

            foreach (BoardSurfacePieces corner in CornerPieces)
            {
                if (!Has(selected, corner))
                    continue;

                if (IsConvex(corner))
                    ClearCorner(output, corner);
                Composite(output, PixelsFor(_spriteSet.GetPiece(corner)));
            }

            // Extend the finalized surface treatment only toward connected cells. Doing this
            // after edge/corner composition preserves lighting across joins as well as alpha.
            BleedInternalSeams(output, mask);

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, false)
            {
                name = "Board Surface " + (byte)mask,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };
            texture.SetPixels32(output);
            texture.Apply(false, true);

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                _spriteSet.LogicalCellSize,
                0,
                SpriteMeshType.FullRect);
            sprite.name = texture.name;
            sprite.hideFlags = HideFlags.DontSave;
            _sprites.Add(mask, sprite);
            return sprite;
        }

        public void Dispose()
        {
            foreach (Sprite sprite in _sprites.Values)
            {
                if (sprite == null)
                    continue;

                Texture2D texture = sprite.texture;
                DestroyObject(sprite);
                DestroyObject(texture);
            }

            _sprites.Clear();
            _sourcePixels.Clear();
        }

        private Color32[] CopyPixels(Sprite sprite)
        {
            Color32[] source = PixelsFor(sprite);
            var copy = new Color32[source.Length];
            Array.Copy(source, copy, source.Length);
            return copy;
        }

        private Color32[] PixelsFor(Sprite sprite)
        {
            if (sprite == null)
                throw new InvalidOperationException("Board surface component sprite is missing.");
            if (_sourcePixels.TryGetValue(sprite, out Color32[] cached))
                return cached;

            int size = _spriteSet.ComponentSize;
            Texture2D texture = sprite.texture;
            Rect rect = sprite.textureRect;
            bool usesFullSourceTexture =
                texture.width == size && texture.height == size;
            if (!usesFullSourceTexture &&
                (Mathf.RoundToInt(rect.width) != size || Mathf.RoundToInt(rect.height) != size))
            {
                throw new InvalidOperationException(
                    $"Board surface sprite '{sprite.name}' must use a {size}x{size} source " +
                    $"texture or atlas rect (texture: {texture.width}x{texture.height}, " +
                    $"rect: {rect.width}x{rect.height}).");
            }

            Color32[] pixels = texture.GetPixels32();
            int textureWidth = texture.width;
            int rectX = usesFullSourceTexture ? 0 : Mathf.RoundToInt(rect.x);
            int rectY = usesFullSourceTexture ? 0 : Mathf.RoundToInt(rect.y);
            var extracted = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                int sourceOffset = (rectY + y) * textureWidth + rectX;
                Array.Copy(pixels, sourceOffset, extracted, y * size, size);
            }

            _sourcePixels.Add(sprite, extracted);
            return extracted;
        }

        private void BleedInternalSeams(
            Color32[] pixels,
            BoardSurfaceNeighborMask mask)
        {
            int overlap = Mathf.Min(
                _spriteSet.SeamOverlap,
                _spriteSet.ComponentPadding);
            if (overlap <= 0)
                return;

            int size = _spriteSet.ComponentSize;
            int min = _spriteSet.ComponentPadding;
            int max = min + _spriteSet.LogicalCellSize - 1;

            if (Has(mask, BoardSurfaceNeighborMask.West))
            {
                for (int y = min; y <= max; y++)
                for (int amount = 1; amount <= overlap; amount++)
                    pixels[y * size + min - amount] = pixels[y * size + min];
            }

            if (Has(mask, BoardSurfaceNeighborMask.East))
            {
                for (int y = min; y <= max; y++)
                for (int amount = 1; amount <= overlap; amount++)
                    pixels[y * size + max + amount] = pixels[y * size + max];
            }

            // PNG rows are top-down while Texture2D pixels are bottom-up. North therefore
            // extends from the high texture row and south from the low texture row.
            if (Has(mask, BoardSurfaceNeighborMask.South))
            {
                for (int x = min; x <= max; x++)
                for (int amount = 1; amount <= overlap; amount++)
                    pixels[(min - amount) * size + x] = pixels[min * size + x];
            }

            if (Has(mask, BoardSurfaceNeighborMask.North))
            {
                for (int x = min; x <= max; x++)
                for (int amount = 1; amount <= overlap; amount++)
                    pixels[(max + amount) * size + x] = pixels[max * size + x];
            }

            BleedConnectedCorner(
                pixels,
                mask,
                BoardSurfaceNeighborMask.North,
                BoardSurfaceNeighborMask.West,
                BoardSurfaceNeighborMask.NorthWest,
                min,
                max,
                -1,
                1,
                overlap,
                size);
            BleedConnectedCorner(
                pixels,
                mask,
                BoardSurfaceNeighborMask.North,
                BoardSurfaceNeighborMask.East,
                BoardSurfaceNeighborMask.NorthEast,
                min,
                max,
                1,
                1,
                overlap,
                size);
            BleedConnectedCorner(
                pixels,
                mask,
                BoardSurfaceNeighborMask.South,
                BoardSurfaceNeighborMask.West,
                BoardSurfaceNeighborMask.SouthWest,
                min,
                max,
                -1,
                -1,
                overlap,
                size);
            BleedConnectedCorner(
                pixels,
                mask,
                BoardSurfaceNeighborMask.South,
                BoardSurfaceNeighborMask.East,
                BoardSurfaceNeighborMask.SouthEast,
                min,
                max,
                1,
                -1,
                overlap,
                size);
        }

        private static void BleedConnectedCorner(
            Color32[] pixels,
            BoardSurfaceNeighborMask mask,
            BoardSurfaceNeighborMask vertical,
            BoardSurfaceNeighborMask horizontal,
            BoardSurfaceNeighborMask diagonal,
            int min,
            int max,
            int directionX,
            int directionY,
            int overlap,
            int size)
        {
            if (!Has(mask, vertical) ||
                !Has(mask, horizontal) ||
                !Has(mask, diagonal))
            {
                return;
            }

            int sourceX = directionX < 0 ? min : max;
            int sourceY = directionY < 0 ? min : max;
            Color32 source = pixels[sourceY * size + sourceX];
            for (int y = 1; y <= overlap; y++)
            for (int x = 1; x <= overlap; x++)
            {
                int targetX = sourceX + directionX * x;
                int targetY = sourceY + directionY * y;
                pixels[targetY * size + targetX] = source;
            }
        }

        private static void Composite(Color32[] bottom, Color32[] top)
        {
            for (int i = 0; i < bottom.Length; i++)
            {
                byte topAlphaByte = top[i].a;
                if (topAlphaByte == 0)
                    continue;
                if (topAlphaByte == 255)
                {
                    bottom[i] = top[i];
                    continue;
                }

                float topAlpha = topAlphaByte / 255f;
                float bottomAlpha = bottom[i].a / 255f;
                float outputAlpha = topAlpha + bottomAlpha * (1f - topAlpha);
                if (outputAlpha <= 0f)
                {
                    bottom[i] = default;
                    continue;
                }

                float bottomWeight = bottomAlpha * (1f - topAlpha);
                bottom[i] = new Color32(
                    ToByte((top[i].r * topAlpha + bottom[i].r * bottomWeight) / outputAlpha),
                    ToByte((top[i].g * topAlpha + bottom[i].g * bottomWeight) / outputAlpha),
                    ToByte((top[i].b * topAlpha + bottom[i].b * bottomWeight) / outputAlpha),
                    ToByte(outputAlpha * 255f));
            }
        }

        private void ClearCorner(Color32[] pixels, BoardSurfacePieces corner)
        {
            int size = _spriteSet.ComponentSize;
            for (int imageY = 0; imageY < size; imageY++)
            {
                int textureY = size - 1 - imageY;
                for (int x = 0; x < size; x++)
                {
                    if (ShouldClear(corner, x + 0.5f, imageY + 0.5f))
                        pixels[textureY * size + x] = default;
                }
            }
        }

        private bool ShouldClear(BoardSurfacePieces corner, float x, float y)
        {
            float left = _spriteSet.ComponentPadding;
            float top = _spriteSet.ComponentPadding;
            float right = left + _spriteSet.LogicalCellSize;
            float bottom = top + _spriteSet.LogicalCellSize;
            float radius = _spriteSet.CornerRadius;
            bool west = IsWest(corner);
            bool north = IsNorth(corner);
            bool convex = IsConvex(corner);

            if (convex)
            {
                float centerX = west ? left + radius : right - radius;
                float centerY = north ? top + radius : bottom - radius;
                bool inX = west ? x < centerX : x > centerX;
                bool inY = north ? y < centerY : y > centerY;
                float dx = x - centerX;
                float dy = y - centerY;
                return inX && inY && dx * dx + dy * dy > radius * radius;
            }

            return false;
        }

        private static bool Has(BoardSurfacePieces selected, BoardSurfacePieces value)
        {
            return (selected & value) != 0;
        }

        private static bool Has(
            BoardSurfaceNeighborMask mask,
            BoardSurfaceNeighborMask value)
        {
            return (mask & value) != 0;
        }

        private static bool IsConvex(BoardSurfacePieces corner)
        {
            return corner == BoardSurfacePieces.ConvexNorthWest ||
                   corner == BoardSurfacePieces.ConvexNorthEast ||
                   corner == BoardSurfacePieces.ConvexSouthWest ||
                   corner == BoardSurfacePieces.ConvexSouthEast;
        }

        private static bool IsWest(BoardSurfacePieces corner)
        {
            return corner == BoardSurfacePieces.ConvexNorthWest ||
                   corner == BoardSurfacePieces.ConvexSouthWest ||
                   corner == BoardSurfacePieces.ConcaveNorthWest ||
                   corner == BoardSurfacePieces.ConcaveSouthWest;
        }

        private static bool IsNorth(BoardSurfacePieces corner)
        {
            return corner == BoardSurfacePieces.ConvexNorthWest ||
                   corner == BoardSurfacePieces.ConvexNorthEast ||
                   corner == BoardSurfacePieces.ConcaveNorthWest ||
                   corner == BoardSurfacePieces.ConcaveNorthEast;
        }

        private static byte ToByte(float value)
        {
            return (byte)Mathf.Clamp(Mathf.RoundToInt(value), 0, 255);
        }

        private static void DestroyObject(Object value)
        {
            if (value == null)
                return;
            if (UnityEngine.Application.isPlaying)
                Object.Destroy(value);
            else
                Object.DestroyImmediate(value);
        }
    }
}
