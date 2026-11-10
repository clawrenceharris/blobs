#!/usr/bin/env python3
"""Bake one board PNG from occupied cells.

The visible board is a single rounded slab. Occupancy is *dilated* by the frame
margin, so the cream border grows outward around the silhouette (and around
holes) instead of being carved out of the edge cells. Every occupied cell then
gets the same centered rounded pad, which keeps the grid a regular lattice that
pieces can sit on. Layouts share this recipe; they are not composited from edge
or corner sprites.

Geometry is an analytic signed distance field built from per-cell rectangles
plus circular corner cuts and fillets. Because it is a real distance field and
not a bitmask, edges resolve with coverage anti-aliasing at any bake
resolution.

Lengths in BAKER_STYLE are expressed against a 256 px reference cell.
"""

from __future__ import annotations

import math
from array import array
from typing import Dict, List, Sequence, Set, Tuple

Point = Tuple[int, int]
RGBA = Tuple[float, float, float, float]

FAR = 1.0e6


BAKER_STYLE = {
    "baker_corner_radius": 51,
    "baker_concave_radius": 40,
    "baker_frame_margin": 16,
    "baker_lip_offset": 24,
    "baker_pad": 160,
    "baker_shadow_offset": [20, 28],
    "baker_shadow_blur": 36,
    "baker_shadow_strength": 0.74,
    "baker_rim_width": 10.0,
    "baker_rim_strength": 0.80,
    "baker_ambient_width": 24.0,
    "baker_ambient_strength": 0.18,
    "baker_cell_gap": 6.0,
    "baker_cell_radius": 28.0,
    "baker_cell_bevel": 9.0,
    "baker_cell_bevel_strength": 0.85,
    "baker_cell_ring": 13.0,
    "baker_cell_ring_strength": 0.18,
    "baker_fill": "#FAF2E5",
    "baker_fill_b": "#F2EBE1",
    "baker_lip_highlight": "#D0BAAC",
    "baker_lip_shade": "#C2A797",
    "baker_lip_tint": "#C9A2D8",
    "baker_lip_tint_strength": 0.60,
    "baker_cell_shadow": "#B8A492",
    "baker_cell_highlight": "#FFFEFB",
}


# The fourth entry mirrors the art concept: a plus arm over a five-wide row and
# an offset bottom row, so the preview can be compared to it directly.
PREVIEW_LAYOUTS: List[Set[Point]] = [
    {(x, y) for x in range(4) for y in range(3)},
    {(x, 3) for x in range(4)} | {(3, y) for y in range(4)},
    {(x, y) for x in range(5) for y in range(4) if not (x == 2 and y in (1, 2))},
    {(1, 0)} | {(x, 1) for x in range(5)} | {(x, 2) for x in range(1, 6)},
    {(0, 0), (1, 0), (2, 0), (2, 1), (2, 2), (3, 2), (4, 2)},
    {(0, 0)},
]


def _hex(value: str, alpha: float = 1.0) -> RGBA:
    value = value.lstrip("#")
    return (
        int(value[0:2], 16) / 255.0,
        int(value[2:4], 16) / 255.0,
        int(value[4:6], 16) / 255.0,
        alpha,
    )


def _clamp01(value: float) -> float:
    return min(1.0, max(0.0, value))


def _smoothstep(edge0: float, edge1: float, value: float) -> float:
    if edge0 == edge1:
        return 0.0
    t = _clamp01((value - edge0) / (edge1 - edge0))
    return t * t * (3.0 - 2.0 * t)


def _over(bottom: RGBA, top: RGBA) -> RGBA:
    ta = _clamp01(top[3])
    ba = _clamp01(bottom[3])
    out_a = ta + ba * (1.0 - ta)
    if out_a <= 0.0:
        return (0.0, 0.0, 0.0, 0.0)
    scale_b = ba * (1.0 - ta)
    return (
        (top[0] * ta + bottom[0] * scale_b) / out_a,
        (top[1] * ta + bottom[1] * scale_b) / out_a,
        (top[2] * ta + bottom[2] * scale_b) / out_a,
        out_a,
    )


def _lerp(a: RGBA, b: RGBA, t: float) -> RGBA:
    t = _clamp01(t)
    return (
        a[0] + (b[0] - a[0]) * t,
        a[1] + (b[1] - a[1]) * t,
        a[2] + (b[2] - a[2]) * t,
        a[3] + (b[3] - a[3]) * t,
    )


def _rounded_rect_sdf(
    x: float,
    y: float,
    center_x: float,
    center_y: float,
    half_width: float,
    half_height: float,
    radius: float,
) -> float:
    qx = abs(x - center_x) - (half_width - radius)
    qy = abs(y - center_y) - (half_height - radius)
    outside = math.hypot(max(qx, 0.0), max(qy, 0.0))
    return outside + min(max(qx, qy), 0.0) - radius


def _merge_style(style: Dict[str, object] | None) -> Dict[str, object]:
    merged: Dict[str, object] = dict(BAKER_STYLE)
    if style:
        merged.update(style)
    return merged


def _occ(occupied: Set[Point], x: int, y: int) -> bool:
    return (x, y) in occupied


def _hull_field(
    width: int,
    height: int,
    occupied: Set[Point],
    min_x: int,
    min_y: int,
    pad: int,
    cell: int,
    margin: float,
    radius: float,
    concave: float,
) -> array:
    """Signed distance to the occupancy hull, dilated outward by ``margin``.

    Negative inside. Convex corners are cut back to ``radius`` and concave
    corners filleted to ``concave``, both measured on the dilated outline.
    """
    field = array("f", [FAR]) * (width * height)

    def rect(i: int, j: int) -> Tuple[float, float, float, float]:
        x0 = pad + (i - min_x) * cell
        y0 = pad + (j - min_y) * cell
        return x0 - margin, y0 - margin, x0 + cell + margin, y0 + cell + margin

    # Pixels are grouped by the cell they land in. A cell whose whole 3x3
    # neighbourhood is occupied is far inside the hull, one with no occupied
    # neighbour is far outside, and only the rest need real distances.
    columns = [min_x + (x - pad) // cell for x in range(width)]
    rows = [min_y + (y - pad) // cell for y in range(height)]
    classified: Dict[Point, Sequence[Tuple[float, float, float, float]] | float] = {}
    for j in set(rows):
        for i in set(columns):
            neighbours = [
                (i + dx, j + dy)
                for dy in (-1, 0, 1)
                for dx in (-1, 0, 1)
                if _occ(occupied, i + dx, j + dy)
            ]
            if len(neighbours) == 9:
                classified[(i, j)] = -FAR
            elif not neighbours:
                classified[(i, j)] = FAR
            else:
                classified[(i, j)] = [rect(*n) for n in neighbours]

    for y in range(height):
        row = y * width
        py = y + 0.5
        j = rows[y]
        for x in range(width):
            entry = classified[(columns[x], j)]
            if isinstance(entry, float):
                field[row + x] = entry
                continue
            px = x + 0.5
            best = FAR
            for x0, y0, x1, y1 in entry:
                qx = max(x0 - px, px - x1)
                qy = max(y0 - py, py - y1)
                if qx > 0.0 or qy > 0.0:
                    distance = math.hypot(max(qx, 0.0), max(qy, 0.0))
                else:
                    distance = max(qx, qy)
                if distance < best:
                    best = distance
            field[row + x] = best

    def cut(cx: float, cy: float, x_min: float, y_min: float, sign: float, r: float) -> None:
        """Intersect (sign 1) or union (sign -1) a disc of radius ``r``."""
        for y in range(max(0, int(y_min)), min(height, int(y_min + r) + 1)):
            row = y * width
            dy = y + 0.5 - cy
            for x in range(max(0, int(x_min)), min(width, int(x_min + r) + 1)):
                dx = x + 0.5 - cx
                disc = math.hypot(dx, dy) - r
                index = row + x
                if sign > 0.0:
                    if disc > field[index]:
                        field[index] = disc
                elif -disc < field[index]:
                    field[index] = -disc

    for i, j in occupied:
        x0, y0, x1, y1 = rect(i, j)
        north = _occ(occupied, i, j - 1)
        east = _occ(occupied, i + 1, j)
        south = _occ(occupied, i, j + 1)
        west = _occ(occupied, i - 1, j)

        if not north and not west:
            cut(x0 + radius, y0 + radius, x0, y0, 1.0, radius)
        if not north and not east:
            cut(x1 - radius, y0 + radius, x1 - radius, y0, 1.0, radius)
        if not south and not west:
            cut(x0 + radius, y1 - radius, x0, y1 - radius, 1.0, radius)
        if not south and not east:
            cut(x1 - radius, y1 - radius, x1 - radius, y1 - radius, 1.0, radius)

        if north and west and not _occ(occupied, i - 1, j - 1):
            cut(x0 - concave, y0 - concave, x0 - concave, y0 - concave, -1.0, concave)
        if north and east and not _occ(occupied, i + 1, j - 1):
            cut(x1 + concave, y0 - concave, x1, y0 - concave, -1.0, concave)
        if south and west and not _occ(occupied, i - 1, j + 1):
            cut(x0 - concave, y1 + concave, x0 - concave, y1, -1.0, concave)
        if south and east and not _occ(occupied, i + 1, j + 1):
            cut(x1 + concave, y1 + concave, x1, y1, -1.0, concave)

    return field


def _coverage(field: array, offset: float = 0.0) -> array:
    values = array("f", [0.0]) * len(field)
    for i, sample in enumerate(field):
        values[i] = _clamp01(0.5 - (sample - offset))
    return values


def _shift(values: array, width: int, height: int, dx: int, dy: int) -> array:
    shifted = array("f", [0.0]) * (width * height)
    for y in range(height):
        sy = y - dy
        if sy < 0 or sy >= height:
            continue
        row = y * width
        source = sy * width
        for x in range(width):
            sx = x - dx
            if 0 <= sx < width:
                shifted[row + x] = values[source + sx]
    return shifted


def _box_blur(values: array, width: int, height: int, radius: int) -> array:
    if radius <= 0:
        return array("f", values)

    horizontal = array("f", [0.0]) * (width * height)
    for y in range(height):
        row = y * width
        prefix = [0.0]
        total = 0.0
        for x in range(width):
            total += values[row + x]
            prefix.append(total)
        for x in range(width):
            lo = max(0, x - radius)
            hi = min(width, x + radius + 1)
            horizontal[row + x] = (prefix[hi] - prefix[lo]) / (hi - lo)

    vertical = array("f", [0.0]) * (width * height)
    for x in range(width):
        prefix = [0.0]
        total = 0.0
        for y in range(height):
            total += horizontal[y * width + x]
            prefix.append(total)
        for y in range(height):
            lo = max(0, y - radius)
            hi = min(height, y + radius + 1)
            vertical[y * width + x] = (prefix[hi] - prefix[lo]) / (hi - lo)
    return vertical


def _tint_lip(base: RGBA, tint: RGBA, strength: float, t: float) -> RGBA:
    """Bounce the world colour into the wall without dragging its value down."""
    mix = _clamp01(strength * (0.45 + 0.55 * _smoothstep(0.0, 1.0, t)))
    mixed = _lerp(base, tint, mix)
    return (mixed[0], mixed[1], mixed[2], 1.0)


def bake_board(occupied: Set[Point], style: Dict[str, object] | None = None, image_cls=None):
    """Return a true-alpha board image for the occupied cell set."""
    if image_cls is None:
        raise TypeError("bake_board requires the project Image type")
    if not occupied:
        raise ValueError("occupied must contain at least one cell")

    style = _merge_style(style)
    cell = int(style["logical_cell_size"])
    scale = cell / 256.0
    radius = float(style["baker_corner_radius"]) * scale
    concave = float(style["baker_concave_radius"]) * scale
    margin = float(style["baker_frame_margin"]) * scale
    lip = max(1, round(float(style["baker_lip_offset"]) * scale))
    pad = max(1, round(float(style["baker_pad"]) * scale))
    shadow_dx, shadow_dy = (round(float(v) * scale) for v in style["baker_shadow_offset"])
    blur = max(1, round(float(style["baker_shadow_blur"]) * scale))
    shadow_strength = float(style["baker_shadow_strength"])
    rim_width = float(style["baker_rim_width"]) * scale
    rim_strength = float(style["baker_rim_strength"])
    ambient_width = float(style["baker_ambient_width"]) * scale
    ambient_strength = float(style["baker_ambient_strength"])
    cell_gap = float(style["baker_cell_gap"]) * scale
    cell_radius = float(style["baker_cell_radius"]) * scale
    bevel = max(1e-3, float(style["baker_cell_bevel"]) * scale)
    bevel_strength = float(style["baker_cell_bevel_strength"])
    ring = max(1e-3, float(style["baker_cell_ring"]) * scale)
    ring_strength = float(style["baker_cell_ring_strength"])

    fill = _hex(str(style["baker_fill"]))
    fill_b = _hex(str(style.get("baker_fill_b", style["baker_fill"])))
    lip_shade = _hex(str(style["baker_lip_shade"]))
    lip_highlight = _hex(str(style["baker_lip_highlight"]))
    lip_tint = _hex(str(style.get("baker_lip_tint", "#C9A2D8")))
    lip_tint_strength = float(style.get("baker_lip_tint_strength", 0.55))
    cell_shadow = _hex(str(style["baker_cell_shadow"]))
    cell_highlight = _hex(str(style["baker_cell_highlight"]))
    palette = style.get("palette") if isinstance(style.get("palette"), dict) else {}
    shadow_rgb = _hex(str(palette.get("shadow", "#37058C")))
    highlight = _hex(str(palette.get("highlight", "#FFFFFA")))
    ambient = _hex(str(palette.get("ambient_edge", "#F3E7D2")))

    min_x = min(point[0] for point in occupied)
    max_x = max(point[0] for point in occupied)
    min_y = min(point[1] for point in occupied)
    max_y = max(point[1] for point in occupied)
    width = (max_x - min_x + 1) * cell + pad * 2
    height = (max_y - min_y + 1) * cell + pad * 2

    hull = _hull_field(
        width, height, occupied, min_x, min_y, pad, cell, margin, radius, concave
    )
    hull_alpha = _coverage(hull)
    lip_alpha = _shift(hull_alpha, width, height, 0, lip)
    body = array("f", [0.0]) * (width * height)
    for i in range(width * height):
        body[i] = max(hull_alpha[i], lip_alpha[i])
    shadow_field = _shift(body, width, height, shadow_dx, shadow_dy)
    for _ in range(3):
        shadow_field = _box_blur(shadow_field, width, height, blur)

    image = image_cls.transparent(width, height)

    for y in range(height):
        row = y * width
        for x in range(width):
            i = row + x
            color: RGBA = (0.0, 0.0, 0.0, 0.0)

            shadow_alpha = shadow_field[i] * shadow_strength
            if shadow_alpha > 0.004:
                color = _over(
                    color,
                    (shadow_rgb[0], shadow_rgb[1], shadow_rgb[2], _clamp01(shadow_alpha)),
                )

            wall = lip_alpha[i]
            if wall > 0.004:
                # Distance below the top face doubles as the wall's depth.
                t = _clamp01(hull[i] / lip)
                shaded = _lerp(lip_highlight, lip_shade, t * t * 0.3 + t * 0.7)
                shaded = _tint_lip(shaded, lip_tint, lip_tint_strength, t)
                color = _over(color, (shaded[0], shaded[1], shaded[2], wall))

            face = hull_alpha[i]
            if face <= 0.004:
                if color[3] > 0.0:
                    image.set(
                        x, y, tuple(round(_clamp01(c) * 255) for c in color)
                    )
                continue

            sample = hull[i]
            nx = 0.0
            ny = 0.0
            if 0 < x < width - 1 and 0 < y < height - 1:
                nx = hull[i + 1] - hull[i - 1]
                ny = hull[i + width] - hull[i - width]
                length = math.hypot(nx, ny)
                if length > 1e-6:
                    nx /= length
                    ny /= length
            # Image y grows south, so north-facing normals have ny < 0.
            facing = _clamp01(-nx * 0.35 - ny * 0.9)
            away = _clamp01(nx * 0.2 + ny * 0.95)

            top = fill
            gx = min_x + (x - pad) // cell
            gy = min_y + (y - pad) // cell
            if _occ(occupied, gx, gy):
                center_x = pad + (gx - min_x) * cell + cell * 0.5
                center_y = pad + (gy - min_y) * cell + cell * 0.5
                half = cell * 0.5 - cell_gap

                def pad_sdf(sx: float, sy: float) -> float:
                    return _rounded_rect_sdf(
                        sx, sy, center_x, center_y, half, half, cell_radius
                    )

                pad_sd = pad_sdf(x + 0.5, y + 0.5)
                crown = 1.0 - _smoothstep(0.0, bevel, -pad_sd)
                trough = _smoothstep(-1.0, 1.0, pad_sd) * (
                    1.0 - _smoothstep(0.0, ring, pad_sd)
                )
                if crown > 0.0 or trough > 0.0:
                    epsilon = 0.75
                    px = pad_sdf(x + 0.5 + epsilon, y + 0.5) - pad_sdf(x + 0.5 - epsilon, y + 0.5)
                    py = pad_sdf(x + 0.5, y + 0.5 + epsilon) - pad_sdf(x + 0.5, y + 0.5 - epsilon)
                    length = math.hypot(px, py)
                    if length > 1e-6:
                        px /= length
                        py /= length
                    lit = _clamp01(-px * 0.75 - py * 0.85)
                    dark = _clamp01(px * 0.45 + py * 0.9)
                else:
                    lit = 0.0
                    dark = 0.0

                inside = _clamp01(0.5 - pad_sd)
                if inside > 0.0:
                    top = _lerp(top, fill if (gx + gy) % 2 == 0 else fill_b, inside)
                # The groove is a shadow in the channel *between* pads, so the
                # pads themselves stay flat and every cell keeps its full area.
                top = _over(
                    top,
                    (
                        cell_shadow[0], cell_shadow[1], cell_shadow[2],
                        ring_strength * (0.7 + 0.3 * dark) * trough,
                    ),
                )
                top = _over(
                    top,
                    (
                        cell_highlight[0], cell_highlight[1], cell_highlight[2],
                        bevel_strength * lit * crown * inside,
                    ),
                )

            # Brightest right at the silhouette, fading inward.
            rim = _smoothstep(-rim_width, 0.0, sample)
            shelf = _smoothstep(-ambient_width, -rim_width, sample) * (
                1.0 - _smoothstep(-rim_width, -rim_width * 0.35, sample)
            )
            top = _over(
                top,
                (ambient[0], ambient[1], ambient[2], ambient_strength * away * shelf),
            )
            top = _over(
                top,
                (
                    highlight[0], highlight[1], highlight[2],
                    rim_strength * rim * (0.25 + 0.75 * facing),
                ),
            )
            color = _over(color, (top[0], top[1], top[2], face))

            if color[3] > 0.0:
                image.set(x, y, tuple(round(_clamp01(c) * 255) for c in color))

    return image


def validate_baker(style: Dict[str, object] | None = None, image_cls=None) -> None:
    if image_cls is None:
        raise TypeError("validate_baker requires the project Image type")
    style = _merge_style(style)
    cell = int(style["logical_cell_size"])
    scale = cell / 256.0
    pad = max(1, round(float(style["baker_pad"]) * scale))
    lip = max(1, round(float(style["baker_lip_offset"]) * scale))
    margin = float(style["baker_frame_margin"]) * scale

    single = bake_board({(0, 0)}, style, image_cls)
    assert single.get(pad + cell // 2, pad + cell // 2)[3] == 255
    # The dilated corner is cut back to the corner radius.
    assert single.get(pad - int(margin), pad - int(margin))[3] < 40

    # The frame is a dilation, so cream exists outside the logical cell on every
    # side. This is the invariant that keeps pads a regular lattice.
    for point in (
        (pad + cell // 2, pad - int(margin * 0.5)),
        (pad - int(margin * 0.5), pad + cell // 2),
        (pad + cell + int(margin * 0.5), pad + cell // 2),
    ):
        assert single.get(*point)[3] == 255, point
    assert single.get(pad + cell // 2, pad - int(margin) - 6)[3] < 200

    south_lip = single.get(pad + cell // 2, pad + cell + int(margin) + lip // 2)
    assert south_lip[3] >= 200
    surface = single.get(pad + cell // 2, pad + cell // 2)
    assert sum(surface[:3]) - sum(south_lip[:3]) >= 40
    # A crust reads as cardboard: the wall stays a thin step under the slab.
    assert single.get(pad + cell // 2, pad + cell + int(margin) + lip + 4)[3] < 200
    contact_shadow = single.get(
        pad + cell // 2,
        min(single.height - 1, pad + cell + int(margin) + lip + 12),
    )
    assert contact_shadow[3] >= 60
    assert single.get(pad + cell // 2, pad - int(margin) - 24)[3] < 90

    donut = {(x, y) for x in range(3) for y in range(3) if not (x == 1 and y == 1)}
    baked_donut = bake_board(donut, style, image_cls)
    hole = baked_donut.get(pad + cell + cell // 2, pad + cell + cell // 2)
    assert hole[3] < 40
    ring = baked_donut.get(pad + cell // 2, pad + cell + cell // 2)
    assert ring[3] == 255
    # The frame wraps the hole too, so the hole is smaller than a cell.
    assert baked_donut.get(pad + cell + int(margin * 0.5), pad + cell + cell // 2)[3] == 255

    pair = bake_board({(0, 0), (1, 0)}, style, image_cls)
    seam = pair.get(pad + cell, pad + cell // 2)
    assert seam[3] == 255
    left = pair.get(pad + cell // 2, pad + cell // 2)
    right = pair.get(pad + cell + cell // 2, pad + cell // 2)
    assert left[:3] != right[:3]
    assert max(abs(left[c] - right[c]) for c in range(3)) <= 16

    # Both cells of the pair carry the same pad: sample symmetric offsets from
    # each cell centre and require the same distance to the pad edge.
    for offset in (int(cell * 0.5 - margin), int(cell * 0.5 - margin)):
        west = pair.get(pad + cell // 2 - offset, pad + cell // 2)
        east = pair.get(pad + cell + cell // 2 + offset, pad + cell // 2)
        assert west[3] == 255 and east[3] == 255

    ell = {(0, 1), (1, 0), (1, 1)}
    baked_ell = bake_board(ell, style, image_cls)
    concave = baked_ell.get(pad + cell - 4, pad + cell - 4)
    assert concave[3] == 255


def render_preview_cards(
    image_cls,
    downsample,
    draw_rounded_rect,
    style: Dict[str, object],
):
    background = _hex(str(style["palette"]["preview_background"]))
    card = _hex(str(style["palette"]["preview_card"]))
    preview = image_cls.solid(2240, 1376, background)
    slot_width, slot_height = 704, 608
    margin_x, margin_y = 48, 56
    for index, occupied in enumerate(PREVIEW_LAYOUTS):
        column = index % 3
        row = index // 3
        card_left = margin_x + column * (slot_width + 16)
        card_top = margin_y + row * (slot_height + 48)
        draw_rounded_rect(preview, card_left, card_top, slot_width, slot_height, 32, card)
        board = downsample(bake_board(occupied, style, image_cls))
        left = card_left + (slot_width - board.width) // 2
        top = card_top + (slot_height - board.height) // 2
        preview.paste_over(board, left, top)
    return preview
