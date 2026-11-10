#!/usr/bin/env python3
"""Bake one board PNG that reads as a pit dug into the ground.

The occupancy hull is the pit opening. Around it a soft clay rim rises out of
the ground with a wobbly outline. Inside, the floor is the opening pushed south
by the wall depth, so the far (north) wall shows as a shaded band while the
side walls are thin and the near wall hides behind the rim. Every occupied cell
gets a felt tile resting on the floor, and the walls cast ambient occlusion
onto the floor and tiles near them.

The opening is extended north by the wall depth, so tiles stay a regular
lattice on the logical grid no matter how deep the wall is.

Lengths in PIT_STYLE are expressed against a 256 px reference cell. Image y
grows south. Keep in step with BoardPitBaker.cs, which bakes at runtime.
"""

from __future__ import annotations

import math
from array import array
from typing import Dict, List, Sequence, Set, Tuple

Point = Tuple[int, int]
RGBA = Tuple[float, float, float, float]

FAR = 1.0e6
_INF = 1.0e20


PIT_STYLE: Dict[str, object] = {
    "logical_cell_size": 256,
    "pit_floor_margin": 12,
    "pit_wall_depth": 54,
    "pit_side_wall": 10,
    "pit_corner_radius": 56,
    "pit_concave_radius": 36,
    "pit_rim_width": 46,
    "pit_rim_height": 26,
    "pit_rim_round": 40,
    "pit_wobble_amplitude": 13,
    "pit_wobble_length": 105,
    "pit_inner_wobble": 0.55,
    "pit_contact_width": 12,
    "pit_contact_strength": 0.85,
    "pit_ao_width": 44,
    "pit_ao_strength": 0.55,
    "pit_tile_gap": 8,
    "pit_tile_radius": 34,
    "pit_tile_bevel": 16,
    "pit_tile_height": 7,
    "pit_tile_shadow_offset": 6,
    "pit_tile_shadow_blur": 10,
    "pit_tile_shadow_strength": 0.30,
    "pit_felt_strength": 0.10,
    "pit_rim_light": "#F2B0E3",
    "pit_rim_mid": "#D47BC2",
    "pit_rim_dark": "#9E4290",
    "pit_contact": "#9E1856",
    "pit_wall_top": "#9A3F8B",
    "pit_wall_bottom": "#62204F",
    "pit_floor": "#98478C",
    "pit_tile_a": "#A45690",
    "pit_tile_b": "#C374B5",
    "pit_ground": "#F8478B",
}

# Light comes from the upper left and mostly toward the viewer, so the crest
# of every rounded form is its brightest line.
_LIGHT = (-0.22, -0.42, 0.88)
_FLAT = _LIGHT[2] / math.sqrt(sum(c * c for c in _LIGHT))


PREVIEW_LAYOUTS: List[Set[Point]] = [
    {(x, y) for x in range(3) for y in range(3)},
    {(x, y) for x in range(4) for y in range(3)},
    {(x, y) for x in range(5) for y in range(4) if not (x == 2 and y in (1, 2))},
    {(1, 0)} | {(x, 1) for x in range(5)} | {(x, 2) for x in range(1, 6)},
    {(0, 0), (1, 0), (2, 0), (2, 1), (2, 2), (3, 2), (4, 2)},
    {(0, 0)},
]


def _hex(value: str) -> Tuple[float, float, float]:
    value = value.lstrip("#")
    return (
        int(value[0:2], 16) / 255.0,
        int(value[2:4], 16) / 255.0,
        int(value[4:6], 16) / 255.0,
    )


def _clamp01(value: float) -> float:
    return min(1.0, max(0.0, value))


def _smoothstep(edge0: float, edge1: float, value: float) -> float:
    if edge0 == edge1:
        return 0.0
    t = _clamp01((value - edge0) / (edge1 - edge0))
    return t * t * (3.0 - 2.0 * t)


def _mix(a: Sequence[float], b: Sequence[float], t: float) -> Tuple[float, float, float]:
    return (
        a[0] + (b[0] - a[0]) * t,
        a[1] + (b[1] - a[1]) * t,
        a[2] + (b[2] - a[2]) * t,
    )


def _scale(color: Sequence[float], factor: float) -> Tuple[float, float, float]:
    return (color[0] * factor, color[1] * factor, color[2] * factor)


def _hash(ix: int, iy: int, seed: int) -> float:
    h = (ix * 374761393 + iy * 668265263 + seed * 2246822519) & 0xFFFFFFFF
    h = ((h ^ (h >> 13)) * 1274126177) & 0xFFFFFFFF
    h ^= h >> 16
    return h / 4294967296.0


def _value_noise(x: float, y: float, seed: int) -> float:
    """Smooth lattice noise in [0, 1)."""
    ix = math.floor(x)
    iy = math.floor(y)
    fx = x - ix
    fy = y - iy
    ux = fx * fx * (3.0 - 2.0 * fx)
    uy = fy * fy * (3.0 - 2.0 * fy)
    a = _hash(ix, iy, seed)
    b = _hash(ix + 1, iy, seed)
    c = _hash(ix, iy + 1, seed)
    d = _hash(ix + 1, iy + 1, seed)
    return a + (b - a) * ux + (c - a) * uy + (a - b - c + d) * ux * uy


def _periodic_noise(x: float, y: float, seed: int, period: int) -> float:
    """Value noise whose lattice wraps every ``period`` cells."""
    ix = math.floor(x)
    iy = math.floor(y)
    fx = x - ix
    fy = y - iy
    ux = fx * fx * (3.0 - 2.0 * fx)
    uy = fy * fy * (3.0 - 2.0 * fy)
    x0 = ix % period
    y0 = iy % period
    x1 = (ix + 1) % period
    y1 = (iy + 1) % period
    a = _hash(x0, y0, seed)
    b = _hash(x1, y0, seed)
    c = _hash(x0, y1, seed)
    d = _hash(x1, y1, seed)
    return a + (b - a) * ux + (c - a) * uy + (a - b - c + d) * ux * uy


GRAIN_TILE = 160


def _grain_table(octaves: Sequence[Tuple[float, int, float]]) -> array:
    """Seamless GRAIN_TILE-square table of centred noise, one entry per pixel.

    Each octave is (pixels per lattice cell, seed, weight); the cell size must
    divide GRAIN_TILE so the table tiles without a seam.
    """
    table = array("f", [0.0]) * (GRAIN_TILE * GRAIN_TILE)
    for y in range(GRAIN_TILE):
        for x in range(GRAIN_TILE):
            value = 0.0
            for size, seed, weight in octaves:
                period = round(GRAIN_TILE / size)
                value += weight * (
                    _periodic_noise((x + 0.5) / size, (y + 0.5) / size, seed, period) - 0.5
                )
            table[y * GRAIN_TILE + x] = value
    return table


_FELT = None
_RIM_GRAIN = None
_WALL_GRAIN = None


def _grain_tables() -> Tuple[array, array, array]:
    global _FELT, _RIM_GRAIN, _WALL_GRAIN
    if _FELT is None:
        _FELT = _grain_table(((1.6, 1, 1.0), (5.0, 2, 0.8)))
        _RIM_GRAIN = _grain_table(((2.5, 3, 1.0),))
        _WALL_GRAIN = _grain_table(((3.2, 5, 1.0),))
    return _FELT, _RIM_GRAIN, _WALL_GRAIN


def _rounded_rect_sdf(
    x: float,
    y: float,
    center_x: float,
    center_y: float,
    half: float,
    radius: float,
) -> float:
    qx = abs(x - center_x) - (half - radius)
    qy = abs(y - center_y) - (half - radius)
    outside = math.hypot(max(qx, 0.0), max(qy, 0.0))
    return outside + min(max(qx, qy), 0.0) - radius


def _rounded_rect_normal(
    x: float,
    y: float,
    center_x: float,
    center_y: float,
    half: float,
    radius: float,
) -> Tuple[float, float]:
    """Outward unit gradient of the rounded-rect distance."""
    dx = x - center_x
    dy = y - center_y
    qx = abs(dx) - (half - radius)
    qy = abs(dy) - (half - radius)
    sx = 1.0 if dx >= 0.0 else -1.0
    sy = 1.0 if dy >= 0.0 else -1.0
    if qx > 0.0 and qy > 0.0:
        length = math.hypot(qx, qy)
        return sx * qx / length, sy * qy / length
    if qx > qy:
        return sx, 0.0
    return 0.0, sy


def _lambert(nx: float, ny: float, nz: float) -> float:
    length = math.sqrt(nx * nx + ny * ny + nz * nz)
    lx, ly, lz = _LIGHT
    light_length = math.sqrt(lx * lx + ly * ly + lz * lz)
    return (nx * lx + ny * ly + nz * lz) / (length * light_length)


def _merge_style(style: Dict[str, object] | None) -> Dict[str, object]:
    merged: Dict[str, object] = dict(PIT_STYLE)
    if style:
        merged.update(style)
    return merged


def _edt_1d(f: List[float], n: int) -> List[float]:
    """Felzenszwalb-Huttenlocher lower envelope of parabolas (squared distance)."""
    d = [0.0] * n
    v = [0] * n
    z = [0.0] * (n + 1)
    k = 0
    z[0] = -_INF
    z[1] = _INF
    for q in range(1, n):
        fq = f[q] + q * q
        p = v[k]
        s = (fq - (f[p] + p * p)) / (2.0 * (q - p))
        while s <= z[k]:
            k -= 1
            p = v[k]
            s = (fq - (f[p] + p * p)) / (2.0 * (q - p))
        k += 1
        v[k] = q
        z[k] = s
        z[k + 1] = _INF
    k = 0
    for q in range(n):
        while z[k + 1] < q:
            k += 1
        p = v[k]
        d[q] = (q - p) * (q - p) + f[p]
    return d


def _distance_to(seeds: bytearray, width: int, height: int) -> array:
    """Euclidean distance from every pixel centre to the nearest seed pixel."""
    grid = array("d", [0.0]) * (width * height)
    column = [0.0] * height
    for x in range(width):
        for y in range(height):
            column[y] = 0.0 if seeds[y * width + x] else _INF
        squared = _edt_1d(column, height)
        for y in range(height):
            grid[y * width + x] = squared[y]
    for y in range(height):
        row = y * width
        squared = _edt_1d(list(grid[row : row + width]), width)
        for x in range(width):
            grid[row + x] = math.sqrt(squared[x])
    return grid


def _signed_distance(inside: bytearray, width: int, height: int) -> array:
    """Signed distance to the mask boundary, negative inside, half-pixel centred."""
    outside = bytearray(1 - value for value in inside)
    to_inside = _distance_to(inside, width, height)
    to_outside = _distance_to(outside, width, height)
    field = array("f", [0.0]) * (width * height)
    for i in range(width * height):
        field[i] = -(to_outside[i] - 0.5) if inside[i] else to_inside[i] - 0.5
    return field


def _threshold(field: array, level: float) -> bytearray:
    return bytearray(1 if value < level else 0 for value in field)


# Distance fields are smooth at the scale of the rim, so they are solved on a
# grid this many times coarser and bilinearly upsampled.
FIELD_STEP = 2


def _opening_field(
    width: int,
    height: int,
    occupied: Set[Point],
    min_x: int,
    min_y: int,
    pad: int,
    cell: int,
    margin: float,
    north: float,
    radius: float,
    concave: float,
) -> array:
    """Signed distance to the pit opening on the coarse grid, in full pixels.

    Each cell's rectangle is dilated by ``margin`` and pushed north by
    ``north`` so the far wall has room without moving the floor lattice.
    A morphological open rounds convex corners to ``radius`` and a close
    fillets concave corners to ``concave``; both keep exact distances away
    from the edge, which the rim and occlusion depend on.
    """
    step = FIELD_STEP
    mask = bytearray(width * height)
    for i, j in occupied:
        x0 = pad + (i - min_x) * cell
        y0 = pad + (j - min_y) * cell
        # A coarse pixel is inside when its centre is.
        left = max(0, math.ceil((x0 - margin) / step - 0.5))
        right = min(width, math.ceil((x0 + cell + margin) / step - 0.5))
        top = max(0, math.ceil((y0 - margin - north) / step - 0.5))
        bottom = min(height, math.ceil((y0 + cell + margin) / step - 0.5))
        for y in range(top, bottom):
            row = y * width
            for x in range(left, right):
                mask[row + x] = 1

    field = _scaled(_signed_distance(mask, width, height), step)
    if radius > 0.0:
        eroded = _scaled(_signed_distance(_threshold(field, -radius), width, height), step)
        field = array("f", (value - radius for value in eroded))
    if concave > 0.0:
        dilated = _scaled(_signed_distance(_threshold(field, concave), width, height), step)
        field = array("f", (value + concave for value in dilated))
    return field


def _scaled(field: array, factor: float) -> array:
    return array("f", (value * factor for value in field))


def _upsample(coarse: array, coarse_width: int, coarse_height: int, width: int, height: int) -> array:
    """Bilinear upsample from the FIELD_STEP grid, sampling at pixel centres."""
    step = FIELD_STEP
    field = array("f", [0.0]) * (width * height)
    xs = []
    for x in range(width):
        u = min(max((x + 0.5) / step - 0.5, 0.0), coarse_width - 1.0)
        x0 = min(int(u), coarse_width - 2) if coarse_width > 1 else 0
        xs.append((x0, u - x0))
    for y in range(height):
        v = min(max((y + 0.5) / step - 0.5, 0.0), coarse_height - 1.0)
        y0 = min(int(v), coarse_height - 2) if coarse_height > 1 else 0
        ty = v - y0
        top = y0 * coarse_width
        bottom = top + coarse_width if coarse_height > 1 else top
        row = y * width
        for x in range(width):
            x0, tx = xs[x]
            x1 = x0 + 1 if coarse_width > 1 else x0
            a = coarse[top + x0] + (coarse[top + x1] - coarse[top + x0]) * tx
            b = coarse[bottom + x0] + (coarse[bottom + x1] - coarse[bottom + x0]) * tx
            field[row + x] = a + (b - a) * ty
    return field


def bake_pit(occupied: Set[Point], style: Dict[str, object] | None = None, image_cls=None):
    """Return a true-alpha pit image for the occupied cell set."""
    if image_cls is None:
        raise TypeError("bake_pit requires the project Image type")
    if not occupied:
        raise ValueError("occupied must contain at least one cell")

    style = _merge_style(style)
    cell = int(style["logical_cell_size"])
    s = cell / 256.0

    def length(key: str) -> float:
        return float(style[key]) * s

    margin = length("pit_floor_margin")
    depth = max(1, round(length("pit_wall_depth")))
    side_wall = length("pit_side_wall")
    radius = length("pit_corner_radius")
    concave = length("pit_concave_radius")
    rim_width = length("pit_rim_width")
    rim_height = length("pit_rim_height")
    wobble_amp = length("pit_wobble_amplitude")
    wobble_len = max(1.0, length("pit_wobble_length"))
    inner_wobble = float(style["pit_inner_wobble"])
    contact_width = max(1e-3, length("pit_contact_width"))
    contact_strength = float(style["pit_contact_strength"])
    ao_width = max(1e-3, length("pit_ao_width"))
    ao_strength = float(style["pit_ao_strength"])
    tile_half = cell * 0.5 - length("pit_tile_gap")
    tile_radius = min(length("pit_tile_radius"), tile_half)
    tile_bevel = max(1e-3, length("pit_tile_bevel"))
    tile_height = length("pit_tile_height")
    tile_shadow_offset = length("pit_tile_shadow_offset")
    tile_shadow_blur = max(1e-3, length("pit_tile_shadow_blur"))
    tile_shadow_strength = float(style["pit_tile_shadow_strength"])
    felt_strength = float(style["pit_felt_strength"])
    rim_round = length("pit_rim_round")
    # The grown rim mask must never touch the image border, or the closing
    # that rounds it cannot erode there.
    pad = math.ceil(
        margin + depth + rim_width + rim_round + wobble_amp * 1.5 + contact_width * 2.0 + 4.0
    )

    rim_light = _hex(str(style["pit_rim_light"]))
    rim_mid = _hex(str(style["pit_rim_mid"]))
    rim_dark = _hex(str(style["pit_rim_dark"]))
    contact = _hex(str(style["pit_contact"]))
    wall_top = _hex(str(style["pit_wall_top"]))
    wall_bottom = _hex(str(style["pit_wall_bottom"]))
    floor = _hex(str(style["pit_floor"]))
    tile_a = _hex(str(style["pit_tile_a"]))
    tile_b = _hex(str(style["pit_tile_b"]))

    min_x = min(p[0] for p in occupied)
    max_x = max(p[0] for p in occupied)
    min_y = min(p[1] for p in occupied)
    max_y = max(p[1] for p in occupied)
    width = (max_x - min_x + 1) * cell + pad * 2
    height = (max_y - min_y + 1) * cell + pad * 2

    coarse_width = math.ceil(width / FIELD_STEP)
    coarse_height = math.ceil(height / FIELD_STEP)
    coarse = _opening_field(
        coarse_width, coarse_height, occupied, min_x, min_y, pad, cell, margin, depth, radius, concave
    )
    opening = _upsample(coarse, coarse_width, coarse_height, width, height)

    # One noise sample drives both edges so the rim reads as one rolled coil.
    wobble = array("f", [0.0]) * (width * height)
    for y in range(height):
        row = y * width
        for x in range(width):
            if abs(opening[row + x]) > rim_width + wobble_amp * 3.0:
                continue
            n = _value_noise(x / wobble_len, y / wobble_len, 7) * 2.0 - 1.0
            n += 0.25 * (_value_noise(x / (wobble_len * 0.45), y / (wobble_len * 0.45), 11) * 2.0 - 1.0)
            wobble[row + x] = n * wobble_amp

    inner = array("f", [0.0]) * (width * height)
    for i in range(width * height):
        inner[i] = opening[i] + wobble[i] * inner_wobble

    # The rim's outer edge is the opening grown by the rim width, closed again
    # so concave corners stay soft instead of pinching to a point.
    grown = _upsample(
        _scaled(
            _signed_distance(_threshold(coarse, rim_width + rim_round), coarse_width, coarse_height),
            FIELD_STEP,
        ),
        coarse_width,
        coarse_height,
        width,
        height,
    )
    outer = array("f", [0.0]) * (width * height)
    for i in range(width * height):
        outer[i] = grown[i] + rim_round - wobble[i]

    # The floor is the opening seen from above and in front: pushed south by
    # the wall depth, then pulled in a little so side walls show too.
    floor_field = array("f", [FAR]) * (width * height)
    for y in range(height):
        row = y * width
        source = (y - depth) * width
        for x in range(width):
            here = inner[row + x]
            if here > 0.0:
                continue
            above = inner[source + x] if y - depth >= 0 else FAR
            floor_field[row + x] = max(above, here) + side_wall

    felt_table, rim_grain, wall_grain = _grain_tables()
    image = image_cls.transparent(width, height)

    # A wide stencil keeps normals smooth over the pixel-quantized distances.
    def field_normal(field: array, x: int, y: int) -> Tuple[float, float]:
        i = y * width + x
        nx = ny = 0.0
        if 2 <= x < width - 2 and 2 <= y < height - 2:
            for o in (-width, 0, width):
                nx += field[i + o + 2] - field[i + o - 2]
            for o in (-1, 0, 1):
                ny += field[i + o + 2 * width] - field[i + o - 2 * width]
        length_ = math.hypot(nx, ny)
        if length_ > 1e-6:
            return nx / length_, ny / length_
        return 0.0, 0.0

    for y in range(height):
        row = y * width
        for x in range(width):
            i = row + x
            sd = inner[i]
            out = outer[i]
            if out > contact_width * 2.0:
                continue

            px = x + 0.5
            py = y + 0.5
            grain_index = (y % GRAIN_TILE) * GRAIN_TILE + x % GRAIN_TILE
            color = (0.0, 0.0, 0.0)
            alpha = 0.0

            # Ground contact: a soft darkening where the rim meets the ground,
            # heavier on the south side where the coil overhangs.
            if out > -1.0:
                nx, ny = field_normal(outer, x, y)
                reach = contact_width * (0.7 + 0.6 * _clamp01(ny))
                color = contact
                alpha = contact_strength * (1.0 - _smoothstep(0.0, reach, out))

            if sd >= -1.0 and out < 1.0:
                # The rim is a rounded coil: a dome profile across its local
                # width, lit from the upper left.
                thickness = max(1.0, sd - out)
                u = _clamp01(sd / thickness)
                slope = rim_height * (math.pi / thickness) * math.cos(math.pi * u)
                nx, ny = field_normal(opening, x, y)
                light = _lambert(-slope * nx, -slope * ny, 1.0)
                lit = _clamp01((light - 0.25) / (_FLAT - 0.25))
                rim = _mix(rim_dark, rim_mid, _smoothstep(0.0, 0.6, lit))
                rim = _mix(rim, rim_light, _smoothstep(0.6, 1.0, lit))
                sheen = math.pow(_clamp01(light), 40.0) * 0.30
                rim = _mix(rim, (1.0, 0.94, 0.98), sheen)
                rim = _scale(rim, 1.0 + rim_grain[grain_index] * felt_strength * 0.5)
                cover = _clamp01(0.5 - out)
                color = _mix(color, rim, cover) if alpha > 0.0 else rim
                alpha = cover + alpha * (1.0 - cover)

            if sd < 1.0:
                f = floor_field[i]
                if f > -1.0:
                    t = _clamp01(f / depth)
                    wall = _mix(wall_bottom, wall_top, math.pow(t, 0.75))
                    wall = _scale(wall, 1.0 + wall_grain[grain_index] * felt_strength)
                else:
                    wall = wall_bottom

                gx = min_x + (x - pad) // cell
                gy = min_y + (y - pad) // cell
                ground = floor
                if (gx, gy) in occupied:
                    cx = pad + (gx - min_x) * cell + cell * 0.5
                    cy = pad + (gy - min_y) * cell + cell * 0.5
                    tile_sd = _rounded_rect_sdf(px, py, cx, cy, tile_half, tile_radius)
                    shadow_sd = _rounded_rect_sdf(
                        px, py - tile_shadow_offset, cx, cy, tile_half, tile_radius
                    )
                    shadow = tile_shadow_strength * (
                        1.0 - _smoothstep(-tile_shadow_blur * 0.5, tile_shadow_blur, shadow_sd)
                    )
                    ground = _mix(floor, wall_bottom, shadow)

                    if tile_sd < 1.0:
                        base = tile_a if (gx + gy) % 2 == 0 else tile_b
                        depth_in = -tile_sd
                        rise = _clamp01(depth_in / tile_bevel)
                        tilt = tile_height / tile_bevel * (1.0 - rise) * (1.0 - rise) * 2.0
                        tx, ty = _rounded_rect_normal(px, py, cx, cy, tile_half, tile_radius)
                        light = _lambert(tilt * tx, tilt * ty, 1.0)
                        tone = 1.0 + (light - _FLAT) * 0.55
                        tile = _scale(base, tone + felt_table[grain_index] * felt_strength)
                        cover = _clamp01(0.5 - tile_sd)
                        ground = _mix(ground, tile, cover)
                else:
                    ground = _scale(ground, 1.0 + wall_grain[grain_index] * felt_strength)

                occlusion = ao_strength * (1.0 - _smoothstep(0.0, ao_width, -f))
                ground = _mix(ground, wall_bottom, occlusion)
                inside = _mix(wall, ground, _clamp01(0.5 - f))

                cover = _clamp01(0.5 - sd)
                if alpha > 0.0:
                    color = _mix(color, inside, cover)
                    alpha = cover + alpha * (1.0 - cover)
                else:
                    color = inside
                    alpha = cover

            if alpha > 0.0:
                image.set(
                    x,
                    y,
                    (
                        round(_clamp01(color[0]) * 255),
                        round(_clamp01(color[1]) * 255),
                        round(_clamp01(color[2]) * 255),
                        round(_clamp01(alpha) * 255),
                    ),
                )

    return image


def render_pit_preview(image_cls, downsample, style: Dict[str, object] | None = None):
    """Lay the preview layouts out on the ground colour."""
    merged = _merge_style(style)
    ground = _hex(str(merged["pit_ground"]))
    preview = image_cls.solid(2240, 1376, (*ground, 1.0))
    slot_width, slot_height = 704, 608
    margin_x, margin_y = 48, 56
    for index, occupied in enumerate(PREVIEW_LAYOUTS):
        column = index % 3
        row = index // 3
        slot_left = margin_x + column * (slot_width + 16)
        slot_top = margin_y + row * (slot_height + 48)
        board = downsample(bake_pit(occupied, merged, image_cls))
        left = slot_left + (slot_width - board.width) // 2
        top = slot_top + (slot_height - board.height) // 2
        preview.paste_over(board, left, top)
    return preview


def main() -> None:
    import sys
    from pathlib import Path

    from generate_board_surface import Image, downsample_half, write_png

    output_dir = Path(__file__).resolve().parent
    preview_path = output_dir.parent / "Preview" / "board_pit_preview.png"
    if "--single" in sys.argv:
        ground = _hex(str(PIT_STYLE["pit_ground"]))
        board = bake_pit(PREVIEW_LAYOUTS[0], {"logical_cell_size": 192}, Image)
        canvas = Image.solid(board.width, board.height, (*ground, 1.0))
        canvas.paste_over(board, 0, 0)
        preview_path = output_dir.parent / "Preview" / "board_pit_single.png"
        write_png(preview_path, canvas)
    else:
        write_png(preview_path, render_pit_preview(Image, downsample_half, {"logical_cell_size": 160}))
    print(f"Generated preview: {preview_path}")


if __name__ == "__main__":
    main()
