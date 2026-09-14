using System;
using System.Collections.Generic;
using Blobs.Content;
using Blobs.Core;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Blobs.Presentation
{
    /// <summary>
    /// Bakes occupied cells into one rounded slab sprite. The occupancy hull is
    /// dilated outward by <see cref="Style.FrameMargin"/>, so the cream border grows
    /// around the silhouette and around holes instead of being carved out of the
    /// edge cells. Every occupied cell then gets the same centered rounded pad,
    /// which keeps the visual grid a regular lattice that pieces sit on.
    ///
    /// Geometry is an analytic signed distance field, not a bitmask, so edges
    /// resolve with coverage anti-aliasing at any bake resolution.
    /// </summary>
    public static class BoardSurfaceBaker
    {
        /// <summary>Lengths in <see cref="Style"/> are authored against this cell size.</summary>
        private const float ReferenceCell = 256f;
        private const float Far = 1e6f;

        [Serializable]
        public struct Style
        {
            public int PixelsPerCell;
            public float CornerRadius;
            public float ConcaveRadius;
            public float FrameMargin;
            public int LipOffset;
            public int Pad;
            public int ShadowOffsetX;

            /// <summary>Pixels the shadow is pushed south. Positive moves it down.</summary>
            public int ShadowOffsetY;
            public int ShadowBlur;
            public float ShadowStrength;
            public float RimWidth;
            public float RimStrength;
            public float AmbientWidth;
            public float AmbientStrength;
            public float CellGap;
            public float CellRadius;
            public float CellBevel;
            public float CellBevelStrength;
            public float CellRing;
            public float CellRingStrength;
            public Color Fill;
            public Color FillB;
            public Color LipShade;
            public Color LipHighlight;
            public Color LipTint;
            public float LipTintStrength;
            public Color CellShadow;
            public Color CellHighlight;
            public Color Shadow;
            public Color Highlight;
            public Color Ambient;
        }

        public sealed class Bake : IDisposable
        {
            public Texture2D Texture { get; }
            public Sprite Sprite { get; }
            public Color32[] Pixels { get; }
            public int Width { get; }
            public int Height { get; }
            public int Pad { get; }
            public int PixelsPerCell { get; }
            public int LipOffset { get; }
            public int FrameMargin { get; }
            public int MinX { get; }
            public int MaxX { get; }
            public int MinY { get; }
            public int MaxY { get; }

            internal Bake(
                Texture2D texture,
                Sprite sprite,
                Color32[] pixels,
                int width,
                int height,
                int pad,
                int pixelsPerCell,
                int lipOffset,
                int frameMargin,
                int minX,
                int maxX,
                int minY,
                int maxY)
            {
                Texture = texture;
                Sprite = sprite;
                Pixels = pixels;
                Width = width;
                Height = height;
                Pad = pad;
                PixelsPerCell = pixelsPerCell;
                LipOffset = lipOffset;
                FrameMargin = frameMargin;
                MinX = minX;
                MaxX = maxX;
                MinY = minY;
                MaxY = maxY;
            }

            public Color32 Get(int x, int y)
            {
                if (x < 0 || y < 0 || x >= Width || y >= Height)
                    return default;
                return Pixels[y * Width + x];
            }

            public void Dispose()
            {
                DestroyObject(Sprite);
                DestroyObject(Texture);
            }

            private static void DestroyObject(Object target)
            {
                if (target == null)
                    return;
                if (UnityEngine.Application.isPlaying)
                    Object.Destroy(target);
                else
                    Object.DestroyImmediate(target);
            }
        }

        /// <summary>
        /// Art-directed defaults. Values mirror
        /// <c>Art/Board/Generated/bake_board_surface.py</c>, which renders the
        /// reviewable preview; keep the two in step.
        /// </summary>
        public static Style DefaultStyle(int pixelsPerCell = 160)
        {
            float scale = pixelsPerCell / ReferenceCell;
            return new Style
            {
                PixelsPerCell = pixelsPerCell,
                CornerRadius = 51f * scale,
                ConcaveRadius = 40f * scale,
                FrameMargin = 16f * scale,
                LipOffset = Mathf.Max(1, Mathf.RoundToInt(24f * scale)),
                Pad = Mathf.Max(8, Mathf.RoundToInt(160f * scale)),
                ShadowOffsetX = Mathf.RoundToInt(20f * scale),
                ShadowOffsetY = Mathf.RoundToInt(28f * scale),
                ShadowBlur = Mathf.Max(1, Mathf.RoundToInt(36f * scale)),
                ShadowStrength = 0.74f,
                RimWidth = 10f * scale,
                RimStrength = 0.80f,
                AmbientWidth = 24f * scale,
                AmbientStrength = 0.18f,
                CellGap = 6f * scale,
                CellRadius = 28f * scale,
                CellBevel = 9f * scale,
                CellBevelStrength = 0.85f,
                CellRing = 13f * scale,
                CellRingStrength = 0.18f,
                Fill = Hex("FAF2E5"),
                FillB = Hex("F2EBE1"),
                LipShade = Hex("C2A797"),
                LipHighlight = Hex("D0BAAC"),
                LipTint = Hex("C9A2D8"),
                LipTintStrength = 0.60f,
                CellShadow = Hex("B8A492"),
                CellHighlight = Hex("FFFEFB"),
                Shadow = Hex("37058C"),
                Highlight = Hex("FFFFFA"),
                Ambient = Hex("F3E7D2")
            };
        }

        public static Style WithPalette(Style style, BoardSurfacePaletteAsset palette)
        {
            if (palette == null)
                return style;

            style.Fill = palette.FillA;
            style.FillB = palette.FillB;
            style.Ambient = palette.AmbientEdge;
            style.Highlight = palette.Highlight;
            style.Shadow = palette.Shadow;
            style.LipShade = palette.LowerEdge;
            style.LipTint = palette.LipTint;
            style.LipTintStrength = palette.LipTintStrength;
            return style;
        }

        public static Bake BakeOccupied(ISet<GridPosition> occupied, Style style)
        {
            if (occupied == null)
                throw new ArgumentNullException(nameof(occupied));
            if (occupied.Count == 0)
                throw new ArgumentException("occupied must contain at least one cell.", nameof(occupied));

            int cell = style.PixelsPerCell;
            float radius = Mathf.Clamp(style.CornerRadius, 1f, cell * 0.5f + style.FrameMargin);
            float concave = Mathf.Clamp(style.ConcaveRadius, 0f, cell * 0.5f);
            float margin = Mathf.Max(0f, style.FrameMargin);
            int lip = Mathf.Max(1, style.LipOffset);
            int pad = Mathf.Max(1, style.Pad);

            int minX = int.MaxValue;
            int maxX = int.MinValue;
            int minY = int.MaxValue;
            int maxY = int.MinValue;
            foreach (GridPosition cellPos in occupied)
            {
                if (cellPos.X < minX) minX = cellPos.X;
                if (cellPos.X > maxX) maxX = cellPos.X;
                if (cellPos.Y < minY) minY = cellPos.Y;
                if (cellPos.Y > maxY) maxY = cellPos.Y;
            }

            int width = (maxX - minX + 1) * cell + pad * 2;
            int height = (maxY - minY + 1) * cell + pad * 2;
            float[] hull = HullField(
                width, height, occupied, minX, minY, pad, cell, margin, radius, concave);

            var hullAlpha = new float[width * height];
            for (int i = 0; i < hullAlpha.Length; i++)
                hullAlpha[i] = Mathf.Clamp01(0.5f - hull[i]);

            // Texture row 0 is the bottom, so shifting south is a negative row step.
            float[] lipAlpha = Shift(hullAlpha, width, height, 0, -lip);
            var body = new float[width * height];
            for (int i = 0; i < body.Length; i++)
                body[i] = Mathf.Max(hullAlpha[i], lipAlpha[i]);
            float[] shadowField = Shift(
                body, width, height, style.ShadowOffsetX, -style.ShadowOffsetY);
            for (int pass = 0; pass < 3; pass++)
                shadowField = BoxBlur(shadowField, width, height, style.ShadowBlur);

            var pixels = new Color32[width * height];
            Vector4 fill = Opaque(style.Fill);
            Vector4 fillB = Opaque(style.FillB);
            Vector4 lipShade = (Vector4)style.LipShade;
            Vector4 lipHighlight = (Vector4)style.LipHighlight;
            Vector4 lipTint = (Vector4)style.LipTint;
            Vector4 cellShadow = (Vector4)style.CellShadow;
            Vector4 cellHighlight = (Vector4)style.CellHighlight;
            Vector4 shadowRgb = (Vector4)style.Shadow;
            Vector4 highlight = (Vector4)style.Highlight;
            Vector4 ambient = (Vector4)style.Ambient;
            float half = cell * 0.5f - style.CellGap;
            float bevel = Mathf.Max(1e-3f, style.CellBevel);
            float ring = Mathf.Max(1e-3f, style.CellRing);
            float cellRadius = Mathf.Clamp(style.CellRadius, 0f, half);

            for (int y = 0; y < height; y++)
            {
                int row = y * width;
                for (int x = 0; x < width; x++)
                {
                    int i = row + x;
                    Vector4 color = Vector4.zero;

                    float shadowAlpha = shadowField[i] * style.ShadowStrength;
                    if (shadowAlpha > 0.004f)
                    {
                        color = Over(color, new Vector4(
                            shadowRgb.x, shadowRgb.y, shadowRgb.z, Mathf.Clamp01(shadowAlpha)));
                    }

                    float wall = lipAlpha[i];
                    if (wall > 0.004f)
                    {
                        // Distance below the top face doubles as the wall's depth.
                        float t = Mathf.Clamp01(hull[i] / lip);
                        Vector4 shaded = Vector4.Lerp(
                            lipHighlight, lipShade, t * t * 0.3f + t * 0.7f);
                        shaded = TintLip(shaded, lipTint, style.LipTintStrength, t);
                        color = Over(color, new Vector4(shaded.x, shaded.y, shaded.z, wall));
                    }

                    float face = hullAlpha[i];
                    if (face <= 0.004f)
                    {
                        pixels[i] = ToColor32(color);
                        continue;
                    }

                    float sample = hull[i];
                    float nx = 0f;
                    float ny = 0f;
                    if (x > 0 && x < width - 1 && y > 0 && y < height - 1)
                    {
                        nx = hull[i + 1] - hull[i - 1];
                        ny = hull[i + width] - hull[i - width];
                        float length = Mathf.Sqrt(nx * nx + ny * ny);
                        if (length > 1e-6f)
                        {
                            nx /= length;
                            ny /= length;
                        }
                    }

                    // Texture y grows north, so north-facing normals have ny > 0.
                    float facing = Mathf.Clamp01(-nx * 0.35f + ny * 0.9f);
                    float away = Mathf.Clamp01(nx * 0.2f - ny * 0.95f);

                    Vector4 top = fill;
                    int gx = minX + DivFloor(x - pad, cell);
                    int gy = minY + DivFloor(y - pad, cell);
                    if (occupied.Contains(new GridPosition(gx, gy)))
                    {
                        float centerX = pad + (gx - minX) * cell + cell * 0.5f;
                        float centerY = pad + (gy - minY) * cell + cell * 0.5f;
                        float cellSd = RoundedRectSdf(
                            x + 0.5f, y + 0.5f, centerX, centerY, half, half, cellRadius);
                        float crown = 1f - Smoothstep(0f, bevel, -cellSd);
                        float trough = Smoothstep(-1f, 1f, cellSd)
                                       * (1f - Smoothstep(0f, ring, cellSd));
                        float lit = 0f;
                        float dark = 0f;
                        if (crown > 0f || trough > 0f)
                        {
                            const float epsilon = 0.75f;
                            float px = RoundedRectSdf(
                                           x + 0.5f + epsilon, y + 0.5f, centerX, centerY,
                                           half, half, cellRadius)
                                       - RoundedRectSdf(
                                           x + 0.5f - epsilon, y + 0.5f, centerX, centerY,
                                           half, half, cellRadius);
                            float py = RoundedRectSdf(
                                           x + 0.5f, y + 0.5f + epsilon, centerX, centerY,
                                           half, half, cellRadius)
                                       - RoundedRectSdf(
                                           x + 0.5f, y + 0.5f - epsilon, centerX, centerY,
                                           half, half, cellRadius);
                            float length = Mathf.Sqrt(px * px + py * py);
                            if (length > 1e-6f)
                            {
                                px /= length;
                                py /= length;
                            }

                            lit = Mathf.Clamp01(-px * 0.75f + py * 0.85f);
                            dark = Mathf.Clamp01(px * 0.45f - py * 0.9f);
                        }

                        float inside = Mathf.Clamp01(0.5f - cellSd);
                        if (inside > 0f)
                        {
                            top = Vector4.Lerp(
                                top, ((gx + gy) & 1) == 0 ? fill : fillB, inside);
                        }

                        // The groove is a shadow in the channel *between* pads, so
                        // the pads stay flat and every cell keeps its full area.
                        top = Over(top, new Vector4(
                            cellShadow.x, cellShadow.y, cellShadow.z,
                            style.CellRingStrength * (0.7f + 0.3f * dark) * trough));
                        top = Over(top, new Vector4(
                            cellHighlight.x, cellHighlight.y, cellHighlight.z,
                            style.CellBevelStrength * lit * crown * inside));
                    }

                    // Brightest right at the silhouette, fading inward.
                    float rim = Smoothstep(-style.RimWidth, 0f, sample);
                    float shelf = Smoothstep(-style.AmbientWidth, -style.RimWidth, sample)
                                  * (1f - Smoothstep(
                                      -style.RimWidth, -style.RimWidth * 0.35f, sample));
                    top = Over(top, new Vector4(
                        ambient.x, ambient.y, ambient.z,
                        style.AmbientStrength * away * shelf));
                    top = Over(top, new Vector4(
                        highlight.x, highlight.y, highlight.z,
                        style.RimStrength * rim * (0.25f + 0.75f * facing)));
                    color = Over(color, new Vector4(top.x, top.y, top.z, face));

                    pixels[i] = ToColor32(color);
                }
            }

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false, false)
            {
                name = "Board Surface Bake",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, false);

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, width, height),
                new Vector2(0.5f, 0.5f),
                cell,
                0,
                SpriteMeshType.FullRect);
            sprite.name = texture.name;
            sprite.hideFlags = HideFlags.DontSave;

            return new Bake(
                texture, sprite, pixels, width, height, pad, cell, lip,
                Mathf.RoundToInt(margin), minX, maxX, minY, maxY);
        }

        /// <summary>
        /// Signed distance to the occupancy hull, dilated outward by <paramref name="margin"/>.
        /// Negative inside. Convex corners are cut back to <paramref name="radius"/> and
        /// concave corners filleted to <paramref name="concave"/>, both on the dilated outline.
        /// </summary>
        private static float[] HullField(
            int width,
            int height,
            ISet<GridPosition> occupied,
            int minX,
            int minY,
            int pad,
            int cell,
            float margin,
            float radius,
            float concave)
        {
            var field = new float[width * height];
            for (int i = 0; i < field.Length; i++)
                field[i] = Far;

            var columns = new int[width];
            for (int x = 0; x < width; x++)
                columns[x] = minX + DivFloor(x - pad, cell);
            var rows = new int[height];
            for (int y = 0; y < height; y++)
                rows[y] = minY + DivFloor(y - pad, cell);

            // Pixels are grouped by the cell they land in. A cell whose whole 3x3
            // neighbourhood is occupied is far inside the hull, one with no occupied
            // neighbour is far outside, and only the rest need real distances.
            var blocks = new Dictionary<GridPosition, Neighbourhood>();
            var neighbours = new List<Rect>(9);
            for (int gy = rows[0]; gy <= rows[height - 1]; gy++)
                for (int gx = columns[0]; gx <= columns[width - 1]; gx++)
                {
                    neighbours.Clear();
                    for (int dy = -1; dy <= 1; dy++)
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            var probe = new GridPosition(gx + dx, gy + dy);
                            if (occupied.Contains(probe))
                                neighbours.Add(CellRect(probe, minX, minY, pad, cell, margin));
                        }

                    blocks.Add(new GridPosition(gx, gy), Neighbourhood.For(neighbours));
                }

            for (int y = 0; y < height; y++)
            {
                int row = y * width;
                float py = y + 0.5f;
                int gy = rows[y];
                for (int x = 0; x < width; x++)
                {
                    Neighbourhood block = blocks[new GridPosition(columns[x], gy)];
                    if (block.Rects == null)
                    {
                        field[row + x] = block.Constant;
                        continue;
                    }

                    float px = x + 0.5f;
                    float best = Far;
                    foreach (Rect rect in block.Rects)
                    {
                        float qx = Mathf.Max(rect.xMin - px, px - rect.xMax);
                        float qy = Mathf.Max(rect.yMin - py, py - rect.yMax);
                        float distance = qx > 0f || qy > 0f
                            ? Mathf.Sqrt(
                                Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f)
                                + Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f))
                            : Mathf.Max(qx, qy);
                        if (distance < best)
                            best = distance;
                    }

                    field[row + x] = best;
                }
            }

            foreach (GridPosition position in occupied)
            {
                Rect rect = CellRect(position, minX, minY, pad, cell, margin);
                float x0 = rect.xMin;
                float y0 = rect.yMin;
                float x1 = rect.xMax;
                float y1 = rect.yMax;
                int i = position.X;
                int j = position.Y;
                bool north = occupied.Contains(new GridPosition(i, j + 1));
                bool east = occupied.Contains(new GridPosition(i + 1, j));
                bool south = occupied.Contains(new GridPosition(i, j - 1));
                bool west = occupied.Contains(new GridPosition(i - 1, j));

                if (!north && !west)
                    Carve(field, width, height, x0 + radius, y1 - radius, x0, y1 - radius, radius, true);
                if (!north && !east)
                    Carve(field, width, height, x1 - radius, y1 - radius, x1 - radius, y1 - radius, radius, true);
                if (!south && !west)
                    Carve(field, width, height, x0 + radius, y0 + radius, x0, y0, radius, true);
                if (!south && !east)
                    Carve(field, width, height, x1 - radius, y0 + radius, x1 - radius, y0, radius, true);

                if (concave <= 0f)
                    continue;
                if (north && west && !occupied.Contains(new GridPosition(i - 1, j + 1)))
                    Carve(field, width, height, x0 - concave, y1 + concave, x0 - concave, y1, concave, false);
                if (north && east && !occupied.Contains(new GridPosition(i + 1, j + 1)))
                    Carve(field, width, height, x1 + concave, y1 + concave, x1, y1, concave, false);
                if (south && west && !occupied.Contains(new GridPosition(i - 1, j - 1)))
                    Carve(field, width, height, x0 - concave, y0 - concave, x0 - concave, y0 - concave, concave, false);
                if (south && east && !occupied.Contains(new GridPosition(i + 1, j - 1)))
                    Carve(field, width, height, x1 + concave, y0 - concave, x1, y0 - concave, concave, false);
            }

            return field;
        }

        /// <summary>
        /// One cell block's contribution to the hull field: either the dilated
        /// rectangles to measure against, or a constant for blocks that are wholly
        /// inside or wholly outside the hull.
        /// </summary>
        private readonly struct Neighbourhood
        {
            public readonly Rect[] Rects;
            public readonly float Constant;

            private Neighbourhood(Rect[] rects, float constant)
            {
                Rects = rects;
                Constant = constant;
            }

            public static Neighbourhood For(List<Rect> neighbours)
            {
                if (neighbours.Count == 9)
                    return new Neighbourhood(null, -Far);
                if (neighbours.Count == 0)
                    return new Neighbourhood(null, Far);
                return new Neighbourhood(neighbours.ToArray(), 0f);
            }
        }

        private static Rect CellRect(
            GridPosition position, int minX, int minY, int pad, int cell, float margin)
        {
            float x0 = pad + (position.X - minX) * cell - margin;
            float y0 = pad + (position.Y - minY) * cell - margin;
            return Rect.MinMaxRect(x0, y0, x0 + cell + margin * 2f, y0 + cell + margin * 2f);
        }

        /// <summary>
        /// Intersects (<paramref name="clip"/>) or unions a disc into the field, but only
        /// inside the corner's own box so neighbouring cells keep their outline.
        /// </summary>
        private static void Carve(
            float[] field,
            int width,
            int height,
            float centerX,
            float centerY,
            float boxX,
            float boxY,
            float radius,
            bool clip)
        {
            int xMin = Mathf.Max(0, Mathf.FloorToInt(boxX));
            int yMin = Mathf.Max(0, Mathf.FloorToInt(boxY));
            int xMax = Mathf.Min(width - 1, Mathf.FloorToInt(boxX + radius));
            int yMax = Mathf.Min(height - 1, Mathf.FloorToInt(boxY + radius));
            for (int y = yMin; y <= yMax; y++)
            {
                int row = y * width;
                float dy = y + 0.5f - centerY;
                for (int x = xMin; x <= xMax; x++)
                {
                    float dx = x + 0.5f - centerX;
                    float disc = Mathf.Sqrt(dx * dx + dy * dy) - radius;
                    int index = row + x;
                    if (clip)
                    {
                        if (disc > field[index])
                            field[index] = disc;
                    }
                    else if (-disc < field[index])
                    {
                        field[index] = -disc;
                    }
                }
            }
        }

        private static float[] Shift(float[] values, int width, int height, int dx, int dy)
        {
            var shifted = new float[width * height];
            for (int y = 0; y < height; y++)
            {
                int sy = y - dy;
                if (sy < 0 || sy >= height)
                    continue;
                int row = y * width;
                int source = sy * width;
                for (int x = 0; x < width; x++)
                {
                    int sx = x - dx;
                    if (sx >= 0 && sx < width)
                        shifted[row + x] = values[source + sx];
                }
            }

            return shifted;
        }

        private static float[] BoxBlur(float[] values, int width, int height, int radius)
        {
            if (radius <= 0)
                return values;

            var horizontal = new float[values.Length];
            var prefix = new float[Mathf.Max(width, height) + 1];
            for (int y = 0; y < height; y++)
            {
                int row = y * width;
                float total = 0f;
                prefix[0] = 0f;
                for (int x = 0; x < width; x++)
                {
                    total += values[row + x];
                    prefix[x + 1] = total;
                }

                for (int x = 0; x < width; x++)
                {
                    int lo = Mathf.Max(0, x - radius);
                    int hi = Mathf.Min(width, x + radius + 1);
                    horizontal[row + x] = (prefix[hi] - prefix[lo]) / (hi - lo);
                }
            }

            var vertical = new float[values.Length];
            for (int x = 0; x < width; x++)
            {
                float total = 0f;
                prefix[0] = 0f;
                for (int y = 0; y < height; y++)
                {
                    total += horizontal[y * width + x];
                    prefix[y + 1] = total;
                }

                for (int y = 0; y < height; y++)
                {
                    int lo = Mathf.Max(0, y - radius);
                    int hi = Mathf.Min(height, y + radius + 1);
                    vertical[y * width + x] = (prefix[hi] - prefix[lo]) / (hi - lo);
                }
            }

            return vertical;
        }

        /// <summary>
        /// Bounces the world colour into the wall without dragging its value down.
        /// </summary>
        private static Vector4 TintLip(Vector4 baseColor, Vector4 tint, float strength, float t)
        {
            float mix = Mathf.Clamp01(strength * (0.45f + 0.55f * Smoothstep(0f, 1f, t)));
            Vector4 mixed = Vector4.Lerp(baseColor, tint, mix);
            return new Vector4(mixed.x, mixed.y, mixed.z, 1f);
        }

        private static float RoundedRectSdf(
            float x,
            float y,
            float centerX,
            float centerY,
            float halfWidth,
            float halfHeight,
            float radius)
        {
            float qx = Mathf.Abs(x - centerX) - (halfWidth - radius);
            float qy = Mathf.Abs(y - centerY) - (halfHeight - radius);
            float outside = Mathf.Sqrt(Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) + Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f));
            return outside + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
        }

        private static Vector4 Over(Vector4 bottom, Vector4 top)
        {
            float ta = Mathf.Clamp01(top.w);
            float ba = Mathf.Clamp01(bottom.w);
            float outA = ta + ba * (1f - ta);
            if (outA <= 0f)
                return Vector4.zero;
            float scaleB = ba * (1f - ta);
            return new Vector4(
                (top.x * ta + bottom.x * scaleB) / outA,
                (top.y * ta + bottom.y * scaleB) / outA,
                (top.z * ta + bottom.z * scaleB) / outA,
                outA);
        }

        private static Vector4 Opaque(Color color)
        {
            Vector4 value = (Vector4)color;
            value.w = 1f;
            return value;
        }

        private static Color32 ToColor32(Vector4 color)
        {
            if (color.w <= 0f)
                return default;
            return new Color32(
                (byte)Mathf.RoundToInt(Mathf.Clamp01(color.x) * 255f),
                (byte)Mathf.RoundToInt(Mathf.Clamp01(color.y) * 255f),
                (byte)Mathf.RoundToInt(Mathf.Clamp01(color.z) * 255f),
                (byte)Mathf.RoundToInt(Mathf.Clamp01(color.w) * 255f));
        }

        private static float Smoothstep(float edge0, float edge1, float value)
        {
            if (Mathf.Approximately(edge0, edge1))
                return 0f;
            float t = Mathf.Clamp01((value - edge0) / (edge1 - edge0));
            return t * t * (3f - 2f * t);
        }

        private static int DivFloor(int value, int divisor)
        {
            if (value >= 0)
                return value / divisor;
            return (value - divisor + 1) / divisor;
        }

        private static Color Hex(string value)
        {
            byte r = Convert.ToByte(value.Substring(0, 2), 16);
            byte g = Convert.ToByte(value.Substring(2, 2), 16);
            byte b = Convert.ToByte(value.Substring(4, 2), 16);
            return new Color(r / 255f, g / 255f, b / 255f, 1f);
        }
    }
}
