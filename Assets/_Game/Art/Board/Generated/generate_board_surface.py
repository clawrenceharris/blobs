#!/usr/bin/env python3
"""Deterministically generate Blobs' modular checkerboard board-surface assets.

The script has no third-party dependencies. Geometry is evaluated from one
shared style dictionary, antialiased with a fixed supersample grid, and written
as true-alpha PNGs with Python's standard library.
"""

from __future__ import annotations

import hashlib
import json
import math
import struct
import zlib
from dataclasses import dataclass
from pathlib import Path
from typing import Callable, Dict, Iterable, List, Sequence, Set, Tuple


RGBA = Tuple[float, float, float, float]
Point = Tuple[int, int]


STYLE = {
    "logical_cell_size": 256,
    "component_size": 384,
    "component_padding": 64,
    "seam_overlap": 2,
    "corner_radius": 48,
    "perimeter_roundness": 48,
    "border_softness": 3,
    "ambient_edge_width": 5,
    "lower_border_width": 18,
    "shadow_offset": [4, 13],
    "shadow_blur": 10,
    "supersample": 4,
    "pixels_per_unit": 256,
    "palette": {
        "fill_a": "#F8F6EF",
        "fill_b": "#F1F0EA",
        "ambient_edge": "#D8D7D1",
        "lower_edge": "#D0CEC7",
        "highlight": "#FFFFFF",
        "shadow": "#30264A",
        "preview_background": "#3B2B68",
        "preview_card": "#4A3976",
    },
}


COMPONENTS = [
    "Fill_A",
    "Fill_B",
    "Edge_N",
    "Edge_E",
    "Edge_S",
    "Edge_W",
    "Corner_Convex_NW",
    "Corner_Convex_NE",
    "Corner_Convex_SW",
    "Corner_Convex_SE",
    "Corner_Concave_NW",
    "Corner_Concave_NE",
    "Corner_Concave_SW",
    "Corner_Concave_SE",
]


CARDINALS = {
    "N": (0, -1),
    "E": (1, 0),
    "S": (0, 1),
    "W": (-1, 0),
}


DIAGONALS = {
    "NW": (-1, -1),
    "NE": (1, -1),
    "SW": (-1, 1),
    "SE": (1, 1),
}


CORNER_CARDINALS = {
    "NW": ("N", "W"),
    "NE": ("N", "E"),
    "SW": ("S", "W"),
    "SE": ("S", "E"),
}


def hex_rgba(value: str, alpha: float = 1.0) -> RGBA:
    value = value.lstrip("#")
    return (
        int(value[0:2], 16) / 255.0,
        int(value[2:4], 16) / 255.0,
        int(value[4:6], 16) / 255.0,
        alpha,
    )


FILL_A = hex_rgba(STYLE["palette"]["fill_a"])
FILL_B = hex_rgba(STYLE["palette"]["fill_b"])
AMBIENT_EDGE = hex_rgba(STYLE["palette"]["ambient_edge"])
LOWER_EDGE = hex_rgba(STYLE["palette"]["lower_edge"])
HIGHLIGHT = hex_rgba(STYLE["palette"]["highlight"])
SHADOW = hex_rgba(STYLE["palette"]["shadow"])


def clamp01(value: float) -> float:
    return min(1.0, max(0.0, value))


def smoothstep(edge0: float, edge1: float, value: float) -> float:
    if edge0 == edge1:
        return 0.0
    t = clamp01((value - edge0) / (edge1 - edge0))
    return t * t * (3.0 - 2.0 * t)


def over(bottom: RGBA, top: RGBA) -> RGBA:
    top_alpha = clamp01(top[3])
    bottom_alpha = clamp01(bottom[3])
    out_alpha = top_alpha + bottom_alpha * (1.0 - top_alpha)
    if out_alpha <= 0.0:
        return (0.0, 0.0, 0.0, 0.0)
    return (
        (top[0] * top_alpha + bottom[0] * bottom_alpha * (1.0 - top_alpha)) / out_alpha,
        (top[1] * top_alpha + bottom[1] * bottom_alpha * (1.0 - top_alpha)) / out_alpha,
        (top[2] * top_alpha + bottom[2] * bottom_alpha * (1.0 - top_alpha)) / out_alpha,
        out_alpha,
    )


def with_alpha(color: RGBA, alpha: float) -> RGBA:
    return (color[0], color[1], color[2], clamp01(alpha))


@dataclass
class Image:
    width: int
    height: int
    pixels: bytearray

    @classmethod
    def transparent(cls, width: int, height: int) -> "Image":
        return cls(width, height, bytearray(width * height * 4))

    @classmethod
    def solid(cls, width: int, height: int, color: RGBA) -> "Image":
        image = cls.transparent(width, height)
        rgba = bytes(round(clamp01(channel) * 255) for channel in color)
        image.pixels[:] = rgba * (width * height)
        return image

    def get(self, x: int, y: int) -> Tuple[int, int, int, int]:
        offset = (y * self.width + x) * 4
        return tuple(self.pixels[offset : offset + 4])  # type: ignore[return-value]

    def set(self, x: int, y: int, color: Tuple[int, int, int, int]) -> None:
        if x < 0 or y < 0 or x >= self.width or y >= self.height:
            return
        offset = (y * self.width + x) * 4
        self.pixels[offset : offset + 4] = bytes(color)

    def clear(self, x: int, y: int) -> None:
        self.set(x, y, (0, 0, 0, 0))

    def paste_over(self, source: "Image", left: int, top: int) -> None:
        for sy in range(source.height):
            dy = sy + top
            if dy < 0 or dy >= self.height:
                continue
            for sx in range(source.width):
                dx = sx + left
                if dx < 0 or dx >= self.width:
                    continue
                sr, sg, sb, sa = source.get(sx, sy)
                if sa == 0:
                    continue
                br, bg, bb, ba = self.get(dx, dy)
                composed = over(
                    (br / 255.0, bg / 255.0, bb / 255.0, ba / 255.0),
                    (sr / 255.0, sg / 255.0, sb / 255.0, sa / 255.0),
                )
                self.set(dx, dy, tuple(round(channel * 255) for channel in composed))


def write_png(path: Path, image: Image) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)

    def chunk(kind: bytes, payload: bytes) -> bytes:
        return (
            struct.pack(">I", len(payload))
            + kind
            + payload
            + struct.pack(">I", zlib.crc32(kind + payload) & 0xFFFFFFFF)
        )

    rows = bytearray()
    stride = image.width * 4
    for y in range(image.height):
        rows.append(0)
        start = y * stride
        rows.extend(image.pixels[start : start + stride])

    png = bytearray(b"\x89PNG\r\n\x1a\n")
    png.extend(chunk(b"IHDR", struct.pack(">IIBBBBB", image.width, image.height, 8, 6, 0, 0, 0)))
    png.extend(chunk(b"IDAT", zlib.compress(bytes(rows), 9)))
    png.extend(chunk(b"IEND", b""))
    path.write_bytes(png)


def render(sample: Callable[[float, float], RGBA]) -> Image:
    size = int(STYLE["component_size"])
    supersample = int(STYLE["supersample"])
    image = Image.transparent(size, size)
    inv_samples = 1.0 / (supersample * supersample)

    for y in range(size):
        for x in range(size):
            premul_r = premul_g = premul_b = alpha = 0.0
            for sy in range(supersample):
                fy = y + (sy + 0.5) / supersample
                for sx in range(supersample):
                    fx = x + (sx + 0.5) / supersample
                    r, g, b, a = sample(fx, fy)
                    premul_r += r * a
                    premul_g += g * a
                    premul_b += b * a
                    alpha += a

            alpha *= inv_samples
            if alpha > 0.0:
                scale = inv_samples / alpha
                color = (
                    round(clamp01(premul_r * scale) * 255),
                    round(clamp01(premul_g * scale) * 255),
                    round(clamp01(premul_b * scale) * 255),
                    round(clamp01(alpha) * 255),
                )
            else:
                color = (0, 0, 0, 0)
            image.set(x, y, color)

    return image


def logical_bounds() -> Tuple[float, float, float, float]:
    pad = float(STYLE["component_padding"])
    size = float(STYLE["logical_cell_size"])
    return pad, pad, pad + size, pad + size


def edge_signed_distance(edge: str, x: float, y: float) -> Tuple[float, float, float]:
    left, top, right, bottom = logical_bounds()
    if edge == "N":
        return top - y, 0.0, -1.0
    if edge == "E":
        return x - right, 1.0, 0.0
    if edge == "S":
        return y - bottom, 0.0, 1.0
    if edge == "W":
        return left - x, -1.0, 0.0
    raise ValueError(edge)


def boundary_treatment(sd: float, nx: float, ny: float) -> RGBA:
    softness = float(STYLE["border_softness"])
    edge_width = float(STYLE["ambient_edge_width"])
    lower_border_width = float(STYLE["lower_border_width"])
    blur = float(STYLE["shadow_blur"])
    offset_x, offset_y = (float(v) for v in STYLE["shadow_offset"])

    light = clamp01(-nx * 0.28 - ny * 0.72)
    # Plate thickness belongs only to downward-facing geometry. Letting the
    # horizontal normal contribute here creates an unwanted gray side border.
    lower = clamp01(ny)
    lower_border_extent = lower_border_width * lower
    shadow_center = nx * offset_x + ny * offset_y

    result: RGBA = (0.0, 0.0, 0.0, 0.0)

    if sd > 0.0:
        shadow_alpha = (0.09 + 0.055 * lower) * math.exp(
            -0.5 * ((sd - shadow_center) / blur) ** 2
        )
        result = over(result, with_alpha(SHADOW, shadow_alpha))

        # A fractional outside cover softens the silhouette without reading as a border.
        outside_cover = 1.0 - smoothstep(0.0, softness, sd)
        result = over(result, with_alpha(AMBIENT_EDGE, 0.16 * outside_cover))

        if sd <= lower_border_extent:
            outer_softness_start = max(0.0, lower_border_extent - softness)
            lower_edge_alpha = 1.0 - smoothstep(
                outer_softness_start,
                lower_border_extent,
                sd,
            )
            result = over(result, with_alpha(LOWER_EDGE, lower_edge_alpha))

    if -edge_width <= sd <= 0.0:
        rim = smoothstep(-edge_width, 0.0, sd)
        result = over(result, with_alpha(AMBIENT_EDGE, (0.08 + 0.06 * lower) * rim))
        result = over(result, with_alpha(HIGHLIGHT, 0.055 * light * rim))

    return result


def sample_fill(color: RGBA, x: float, y: float) -> RGBA:
    left, top, right, bottom = logical_bounds()
    if left <= x <= right and top <= y <= bottom:
        return color
    return (0.0, 0.0, 0.0, 0.0)


def sample_edge(edge: str, x: float, y: float) -> RGBA:
    left, top, right, bottom = logical_bounds()
    if edge in ("N", "S") and not (left <= x < right):
        return (0.0, 0.0, 0.0, 0.0)
    if edge in ("E", "W") and not (top <= y < bottom):
        return (0.0, 0.0, 0.0, 0.0)
    return boundary_treatment(*edge_signed_distance(edge, x, y))


def corner_geometry(kind: str, corner: str, x: float, y: float) -> Tuple[float, float, float, bool]:
    left, top, right, bottom = logical_bounds()
    radius = float(STYLE["corner_radius"])
    margin = float(STYLE["border_softness"] + 3 * STYLE["shadow_blur"])

    if kind == "Convex":
        center_x = left + radius if "W" in corner else right - radius
        center_y = top + radius if "N" in corner else bottom - radius
        direction_x = -1.0 if "W" in corner else 1.0
        direction_y = -1.0 if "N" in corner else 1.0
        in_region = (
            0.0 <= direction_x * (x - center_x) <= radius + margin
            and 0.0 <= direction_y * (y - center_y) <= radius + margin
        )
        vx = x - center_x
        vy = y - center_y
        distance = math.hypot(vx, vy)
        if distance <= 1e-6:
            nx, ny = direction_x, direction_y
        else:
            nx, ny = vx / distance, vy / distance
        return distance - radius, nx, ny, in_region

    vertex_x = left if "W" in corner else right
    vertex_y = top if "N" in corner else bottom
    missing_x = -1.0 if "W" in corner else 1.0
    missing_y = -1.0 if "N" in corner else 1.0
    center_x = vertex_x + missing_x * radius
    center_y = vertex_y + missing_y * radius
    in_region = (
        0.0 <= missing_x * (x - vertex_x) <= radius
        and 0.0 <= missing_y * (y - vertex_y) <= radius
    )
    vx = x - center_x
    vy = y - center_y
    distance = math.hypot(vx, vy)
    if distance <= 1e-6:
        nx, ny = -missing_x, -missing_y
    else:
        # Concave board-space outward normal points toward the hole center.
        nx, ny = -vx / distance, -vy / distance
    return radius - distance, nx, ny, in_region


def sample_corner(kind: str, corner: str, x: float, y: float) -> RGBA:
    sd, nx, ny, in_region = corner_geometry(kind, corner, x, y)
    if not in_region:
        return (0.0, 0.0, 0.0, 0.0)
    return boundary_treatment(sd, nx, ny)


def render_components() -> Dict[str, Image]:
    images: Dict[str, Image] = {
        "Fill_A": render(lambda x, y: sample_fill(FILL_A, x, y)),
        "Fill_B": render(lambda x, y: sample_fill(FILL_B, x, y)),
    }
    for edge in CARDINALS:
        images[f"Edge_{edge}"] = render(lambda x, y, edge=edge: sample_edge(edge, x, y))
    for kind in ("Convex", "Concave"):
        for corner in ("NW", "NE", "SW", "SE"):
            name = f"Corner_{kind}_{corner}"
            images[name] = render(
                lambda x, y, kind=kind, corner=corner: sample_corner(kind, corner, x, y)
            )
    return images


def has_neighbor(occupied: Set[Point], cell: Point, offset: Point) -> bool:
    return (cell[0] + offset[0], cell[1] + offset[1]) in occupied


def selection_for(occupied: Set[Point], cell: Point) -> List[str]:
    present = {name: has_neighbor(occupied, cell, offset) for name, offset in CARDINALS.items()}
    pieces: List[str] = ["Fill_A" if (cell[0] + cell[1]) % 2 == 0 else "Fill_B"]
    for edge in ("N", "E", "S", "W"):
        if not present[edge]:
            pieces.append(f"Edge_{edge}")
    for corner, (first, second) in CORNER_CARDINALS.items():
        if not present[first] and not present[second]:
            pieces.append(f"Corner_Convex_{corner}")
        elif present[first] and present[second] and not has_neighbor(occupied, cell, DIAGONALS[corner]):
            pieces.append(f"Corner_Concave_{corner}")
    return pieces


def should_clear(kind: str, corner: str, x: float, y: float) -> bool:
    left, top, right, bottom = logical_bounds()
    radius = float(STYLE["corner_radius"])
    if kind == "Convex":
        cx = left + radius if "W" in corner else right - radius
        cy = top + radius if "N" in corner else bottom - radius
        in_x = x < cx if "W" in corner else x > cx
        in_y = y < cy if "N" in corner else y > cy
        return in_x and in_y and math.hypot(x - cx, y - cy) > radius

    return False


def should_fill_concave(corner: str, x: float, y: float) -> bool:
    left, top, right, bottom = logical_bounds()
    radius = float(STYLE["corner_radius"])
    vertex_x = left if "W" in corner else right
    vertex_y = top if "N" in corner else bottom
    direction_x = -1.0 if "W" in corner else 1.0
    direction_y = -1.0 if "N" in corner else 1.0
    center_x = vertex_x + direction_x * radius
    center_y = vertex_y + direction_y * radius
    offset_x = direction_x * (x - vertex_x)
    offset_y = direction_y * (y - vertex_y)
    return (
        0.0 < offset_x <= radius
        and 0.0 < offset_y <= radius
        and math.hypot(x - center_x, y - center_y) >= radius
    )


def bleed_flat_fill(output: Image, pieces: Sequence[str]) -> None:
    """Mirror BoardSurfaceSpriteComposer's pre-overlay seam bleed exactly."""
    overlap = min(int(STYLE["seam_overlap"]), int(STYLE["component_padding"]))
    if overlap <= 0:
        return

    minimum = int(STYLE["component_padding"])
    maximum = minimum + int(STYLE["logical_cell_size"]) - 1
    present = {
        edge: f"Edge_{edge}" not in pieces
        for edge in ("N", "E", "S", "W")
    }

    if present["W"]:
        for y in range(minimum, maximum + 1):
            for amount in range(1, overlap + 1):
                output.set(minimum - amount, y, output.get(minimum, y))
    if present["E"]:
        for y in range(minimum, maximum + 1):
            for amount in range(1, overlap + 1):
                output.set(maximum + amount, y, output.get(maximum, y))
    if present["N"]:
        for x in range(minimum, maximum + 1):
            for amount in range(1, overlap + 1):
                output.set(x, minimum - amount, output.get(x, minimum))
    if present["S"]:
        for x in range(minimum, maximum + 1):
            for amount in range(1, overlap + 1):
                output.set(x, maximum + amount, output.get(x, maximum))

    for corner, (first, second) in CORNER_CARDINALS.items():
        diagonal_present = (
            present[first]
            and present[second]
            and f"Corner_Concave_{corner}" not in pieces
        )
        if not diagonal_present:
            continue

        source_x = minimum if "W" in corner else maximum
        source_y = minimum if "N" in corner else maximum
        direction_x = -1 if "W" in corner else 1
        direction_y = -1 if "N" in corner else 1
        source = output.get(source_x, source_y)
        for y in range(1, overlap + 1):
            for x in range(1, overlap + 1):
                output.set(
                    source_x + direction_x * x,
                    source_y + direction_y * y,
                    source,
                )


def compose_cell(images: Dict[str, Image], pieces: Sequence[str]) -> Image:
    size = int(STYLE["component_size"])
    output = Image.transparent(size, size)
    fill_name = pieces[0]
    output.paste_over(images[fill_name], 0, 0)
    bleed_flat_fill(output, pieces)
    fill_color = images[fill_name].get(
        int(STYLE["component_padding"] + STYLE["logical_cell_size"] // 2),
        int(STYLE["component_padding"] + STYLE["logical_cell_size"] // 2),
    )

    for piece in pieces:
        if piece.startswith("Edge_"):
            output.paste_over(images[piece], 0, 0)

    corners = [piece for piece in pieces if piece.startswith("Corner_")]
    for piece in corners:
        _, kind, corner = piece.split("_")
        if kind == "Convex":
            for y in range(size):
                for x in range(size):
                    if should_clear(kind, corner, x + 0.5, y + 0.5):
                        output.clear(x, y)
        else:
            for y in range(size):
                for x in range(size):
                    if should_fill_concave(corner, x + 0.5, y + 0.5):
                        output.set(x, y, fill_color)
        output.paste_over(images[piece], 0, 0)

    return output


def downsample_half(source: Image) -> Image:
    result = Image.transparent(source.width // 2, source.height // 2)
    for y in range(result.height):
        for x in range(result.width):
            samples = [source.get(x * 2 + dx, y * 2 + dy) for dy in (0, 1) for dx in (0, 1)]
            alpha_sum = sum(pixel[3] for pixel in samples)
            if alpha_sum == 0:
                continue
            color = (
                round(sum(pixel[0] * pixel[3] for pixel in samples) / alpha_sum),
                round(sum(pixel[1] * pixel[3] for pixel in samples) / alpha_sum),
                round(sum(pixel[2] * pixel[3] for pixel in samples) / alpha_sum),
                round(alpha_sum / 4),
            )
            result.set(x, y, color)
    return result


def draw_rounded_rect(image: Image, left: int, top: int, width: int, height: int, radius: int, color: RGBA) -> None:
    right = left + width
    bottom = top + height
    rgba = tuple(round(channel * 255) for channel in color)
    for y in range(max(0, top), min(image.height, bottom)):
        for x in range(max(0, left), min(image.width, right)):
            cx = min(max(x + 0.5, left + radius), right - radius)
            cy = min(max(y + 0.5, top + radius), bottom - radius)
            if math.hypot(x + 0.5 - cx, y + 0.5 - cy) <= radius:
                image.set(x, y, rgba)


def render_board(
    images: Dict[str, Image],
    occupied: Set[Point],
    composed_cache: Dict[Tuple[str, ...], Image] | None = None,
) -> Image:
    cell_size = int(STYLE["logical_cell_size"])
    pad = int(STYLE["component_padding"])
    min_x = min(cell[0] for cell in occupied)
    max_x = max(cell[0] for cell in occupied)
    min_y = min(cell[1] for cell in occupied)
    max_y = max(cell[1] for cell in occupied)
    width = (max_x - min_x + 1) * cell_size + pad * 2
    height = (max_y - min_y + 1) * cell_size + pad * 2
    board = Image.transparent(width, height)
    cache = composed_cache if composed_cache is not None else {}

    for cell in sorted(occupied, key=lambda p: (p[1], p[0])):
        pieces = selection_for(occupied, cell)
        key = tuple(pieces)
        component = cache.get(key)
        if component is None:
            component = compose_cell(images, pieces)
            cache[key] = component
        left = (cell[0] - min_x) * cell_size
        top = (cell[1] - min_y) * cell_size
        board.paste_over(component, left, top)

    return board


def render_preview(images: Dict[str, Image]) -> Image:
    configurations: List[Set[Point]] = [
        {(x, y) for x in range(4) for y in range(3)},
        {(x, 3) for x in range(4)} | {(3, y) for y in range(4)},
        {(x, y) for x in range(5) for y in range(4) if not (x == 2 and y in (1, 2))},
        {(x, y) for x in range(5) for y in range(4)} - {(4, 0), (3, 0), (4, 1)},
        {(0, 0), (1, 0), (2, 0), (2, 1), (2, 2), (3, 2), (4, 2)},
        {(0, 0)},
    ]

    background = hex_rgba(STYLE["palette"]["preview_background"])
    card = hex_rgba(STYLE["palette"]["preview_card"])
    preview = Image.solid(2240, 1376, background)
    slot_width, slot_height = 704, 608
    margin_x, margin_y = 48, 56
    composed_cache: Dict[Tuple[str, ...], Image] = {}

    for index, occupied in enumerate(configurations):
        column = index % 3
        row = index // 3
        card_left = margin_x + column * (slot_width + 16)
        card_top = margin_y + row * (slot_height + 48)
        draw_rounded_rect(preview, card_left, card_top, slot_width, slot_height, 32, card)

        board = downsample_half(render_board(images, occupied, composed_cache))
        left = card_left + (slot_width - board.width) // 2
        top = card_top + (slot_height - board.height) // 2
        preview.paste_over(board, left, top)

    return preview


def deterministic_guid(relative_path: str) -> str:
    return hashlib.md5(("blobs-board-surface:" + relative_path).encode("utf-8")).hexdigest()


def find_project_root(start: Path) -> Path:
    for candidate in (start, *start.parents):
        if (candidate / "ProjectSettings/ProjectVersion.txt").is_file():
            return candidate
    raise RuntimeError(f"Could not find the Unity project root above {start}")


def guid_for_asset(path: Path, project_root: Path) -> str:
    meta_path = path.with_name(path.name + ".meta")
    if meta_path.is_file():
        for line in meta_path.read_text(encoding="utf-8").splitlines():
            if line.startswith("guid: "):
                return line.removeprefix("guid: ").strip()
    return deterministic_guid(path.relative_to(project_root).as_posix())


def write_text_meta(path: Path, project_root: Path) -> None:
    path.with_suffix(path.suffix + ".meta").write_text(
        "fileFormatVersion: 2\n"
        f"guid: {guid_for_asset(path, project_root)}\n"
        "TextScriptImporter:\n"
        "  externalObjects: {}\n"
        "  userData: \n"
        "  assetBundleName: \n"
        "  assetBundleVariant: \n",
        encoding="utf-8",
    )


def write_sprite_meta(path: Path, project_root: Path, readable: bool = True) -> None:
    guid = guid_for_asset(path, project_root)
    sprite_id = hashlib.md5((guid + ":sprite").encode("utf-8")).hexdigest()[:16] + "0800000000000000"
    path.with_suffix(path.suffix + ".meta").write_text(
        f"""fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: {1 if readable else 0}
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 0
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: {STYLE['pixels_per_unit']}
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 0
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 100
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    customData: 
    physicsShape: []
    bones: []
    spriteID: {sprite_id}
    internalID: 0
    vertices: []
    indices: 
    edges: []
    weights: []
    secondaryTextures: []
    spriteCustomMetadata:
      entries: []
    nameFileIdTable: {{}}
  mipmapLimitGroupName: 
  pSDRemoveMatte: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
""",
        encoding="utf-8",
    )


def write_folder_meta(path: Path, project_root: Path) -> None:
    relative = path.relative_to(project_root).as_posix()
    meta = path.with_name(path.name + ".meta")
    if meta.exists():
        return
    meta.write_text(
        "fileFormatVersion: 2\n"
        f"guid: {deterministic_guid(relative)}\n"
        "folderAsset: yes\n"
        "DefaultImporter:\n"
        "  externalObjects: {}\n"
        "  userData: \n"
        "  assetBundleName: \n"
        "  assetBundleVariant: \n",
        encoding="utf-8",
    )


def write_atlas(images: Dict[str, Image], output_dir: Path) -> Tuple[Path, Dict[str, object]]:
    component_size = int(STYLE["component_size"])
    columns = 4
    rows = 4
    atlas = Image.transparent(component_size * columns, component_size * rows)
    mapping: Dict[str, object] = {
        "image": "board_components_atlas.png",
        "coordinate_origin": "top-left",
        "atlas_size": [atlas.width, atlas.height],
        "logical_cell_size": [STYLE["logical_cell_size"], STYLE["logical_cell_size"]],
        "component_size": [component_size, component_size],
        "components": {},
    }

    for index, name in enumerate(COMPONENTS):
        column = index % columns
        row = index // columns
        x = column * component_size
        y = row * component_size
        atlas.paste_over(images[name], x, y)
        mapping["components"][name] = {
            "rect": [x, y, component_size, component_size],
            "uv": [x / atlas.width, y / atlas.height, component_size / atlas.width, component_size / atlas.height],
        }

    path = output_dir / "board_components_atlas.png"
    write_png(path, atlas)
    return path, mapping


def write_runtime_asset(project_root: Path, output_dir: Path, asset_dir: Path) -> None:
    asset_dir.mkdir(parents=True, exist_ok=True)
    write_folder_meta(asset_dir.parent, project_root)
    write_folder_meta(asset_dir, project_root)

    asset_path = asset_dir / "BoardSurfaceSpriteSet.asset"
    asset_guid = guid_for_asset(asset_path, project_root)
    script_path = project_root / "Assets/_Game/Scripts/Presentation/BoardSurface/BoardSurfaceSpriteSet.cs"
    script_guid = guid_for_asset(script_path, project_root)

    field_names = {
        "Fill_A": "fillA",
        "Fill_B": "fillB",
        "Edge_N": "edgeN",
        "Edge_E": "edgeE",
        "Edge_S": "edgeS",
        "Edge_W": "edgeW",
        "Corner_Convex_NW": "convexNW",
        "Corner_Convex_NE": "convexNE",
        "Corner_Convex_SW": "convexSW",
        "Corner_Convex_SE": "convexSE",
        "Corner_Concave_NW": "concaveNW",
        "Corner_Concave_NE": "concaveNE",
        "Corner_Concave_SW": "concaveSW",
        "Corner_Concave_SE": "concaveSE",
    }
    references = []
    for name in COMPONENTS:
        png_path = output_dir / f"{name}.png"
        guid = guid_for_asset(png_path, project_root)
        references.append(f"  {field_names[name]}: {{fileID: 21300000, guid: {guid}, type: 3}}")

    asset_path.write_text(
        "%YAML 1.1\n"
        "%TAG !u! tag:unity3d.com,2011:\n"
        "--- !u!114 &11400000\n"
        "MonoBehaviour:\n"
        "  m_ObjectHideFlags: 0\n"
        "  m_CorrespondingSourceObject: {fileID: 0}\n"
        "  m_PrefabInstance: {fileID: 0}\n"
        "  m_PrefabAsset: {fileID: 0}\n"
        "  m_GameObject: {fileID: 0}\n"
        "  m_Enabled: 1\n"
        "  m_EditorHideFlags: 0\n"
        f"  m_Script: {{fileID: 11500000, guid: {script_guid}, type: 3}}\n"
        "  m_Name: BoardSurfaceSpriteSet\n"
        "  m_EditorClassIdentifier: Blobs.Presentation::Blobs.Presentation.BoardSurfaceSpriteSet\n"
        + "\n".join(references)
        + "\n"
        f"  logicalCellSize: {STYLE['logical_cell_size']}\n"
        f"  componentSize: {STYLE['component_size']}\n"
        f"  componentPadding: {STYLE['component_padding']}\n"
        f"  seamOverlap: {STYLE['seam_overlap']}\n"
        f"  cornerRadius: {STYLE['corner_radius']}\n",
        encoding="utf-8",
    )
    asset_path.with_suffix(".asset.meta").write_text(
        "fileFormatVersion: 2\n"
        f"guid: {asset_guid}\n"
        "NativeFormatImporter:\n"
        "  externalObjects: {}\n"
        "  mainObjectFileID: 11400000\n"
        "  userData: \n"
        "  assetBundleName: \n"
        "  assetBundleVariant: \n",
        encoding="utf-8",
    )


def write_manifest_files(project_root: Path, output_dir: Path, atlas_mapping: Dict[str, object]) -> None:
    style_path = output_dir / "board_surface_style.json"
    style_path.write_text(json.dumps(STYLE, indent=2) + "\n", encoding="utf-8")
    mapping_path = output_dir / "board_components_atlas.json"
    mapping_path.write_text(json.dumps(atlas_mapping, indent=2) + "\n", encoding="utf-8")
    write_text_meta(style_path, project_root)
    write_text_meta(mapping_path, project_root)


def validate(images: Dict[str, Image], preview: Image) -> None:
    size = int(STYLE["component_size"])
    pad = int(STYLE["component_padding"])
    cell = int(STYLE["logical_cell_size"])

    assert list(images) == COMPONENTS
    assert STYLE["perimeter_roundness"] == STYLE["corner_radius"]
    assert all(image.width == size and image.height == size for image in images.values())
    assert images["Fill_A"].get(0, 0)[3] == 0
    assert images["Fill_B"].get(0, 0)[3] == 0
    assert images["Fill_A"].get(pad + cell // 2, pad + cell // 2)[3] == 255
    assert images["Fill_B"].get(pad + cell // 2, pad + cell // 2)[3] == 255
    assert images["Fill_A"].get(pad + cell // 2, pad + cell // 2) != images["Fill_B"].get(
        pad + cell // 2, pad + cell // 2
    )
    # The reference uses flat uninterrupted checker cells. Any accidental
    # per-cell gradient, inset pad, grain, or facet must fail generation.
    for name in ("Fill_A", "Fill_B"):
        fill = images[name]
        expected = fill.get(pad + cell // 2, pad + cell // 2)
        for y in range(pad, pad + cell):
            for x in range(pad, pad + cell):
                assert fill.get(x, y) == expected
    assert all(
        any(image.pixels[index] == 0 for index in range(3, len(image.pixels), 4))
        for image in images.values()
    )
    assert preview.width == 2240 and preview.height == 1376

    south_border = images["Edge_S"].get(
        pad + cell // 2,
        pad + cell + 6,
    )
    assert south_border[3] >= 240

    # Thickness is directional: only the lower edge may become an opaque lip.
    # The other sides contain just soft antialiasing/shadow and cannot turn
    # into the dark gray outline seen in the failed iteration.
    north_outside = images["Edge_N"].get(pad + cell // 2, pad - 6)
    east_outside = images["Edge_E"].get(pad + cell + 6, pad + cell // 2)
    west_outside = images["Edge_W"].get(pad - 6, pad + cell // 2)
    assert max(north_outside[3], east_outside[3], west_outside[3]) < 96

    concave_tangent_guard = images["Corner_Concave_NW"].get(
        pad - int(STYLE["corner_radius"]) - 8,
        pad - 1,
    )
    assert concave_tangent_guard[3] == 0

    # Tangential samples must be identical, guaranteeing exact straight-edge joins.
    for edge in ("N", "S"):
        image = images[f"Edge_{edge}"]
        for y in range(size):
            assert image.get(pad + 8, y) == image.get(pad + cell - 9, y)
    for edge in ("E", "W"):
        image = images[f"Edge_{edge}"]
        for x in range(size):
            assert image.get(x, pad + 8) == image.get(x, pad + cell - 9)


def main() -> None:
    output_dir = Path(__file__).resolve().parent
    project_root = find_project_root(output_dir)
    preview_dir = output_dir.parent / "Preview"
    preview_dir.mkdir(parents=True, exist_ok=True)

    for folder in (
        output_dir.parent.parent,
        output_dir.parent,
        output_dir,
        preview_dir,
    ):
        write_folder_meta(folder, project_root)

    images = render_components()

    # Keep the directory aligned with this generator's intentionally small
    # component set. This also removes assets from abandoned style experiments.
    obsolete_paths = [output_dir / "Fill.png", output_dir / "Fill.png.meta"]
    for pattern in ("Corner_ConvexCap_*.png*", "Rim_*.png*"):
        obsolete_paths.extend(output_dir.glob(pattern))
    for obsolete in obsolete_paths:
        if obsolete.is_file():
            obsolete.unlink()

    for name in COMPONENTS:
        path = output_dir / f"{name}.png"
        write_png(path, images[name])
        write_sprite_meta(path, project_root, readable=True)

    atlas_path, mapping = write_atlas(images, output_dir)
    write_sprite_meta(atlas_path, project_root, readable=False)
    write_manifest_files(project_root, output_dir, mapping)

    preview = render_preview(images)
    preview_path = preview_dir / "board_atlas_preview.png"
    write_png(preview_path, preview)
    write_sprite_meta(preview_path, project_root, readable=False)
    style_reference_path = preview_dir / "board_style_reference.png"
    if style_reference_path.exists():
        write_sprite_meta(style_reference_path, project_root, readable=False)

    write_runtime_asset(
        project_root,
        output_dir,
        project_root / "Assets/_Game/Content/Board",
    )
    write_runtime_asset(
        project_root,
        output_dir,
        project_root / "Assets/Resources/Board",
    )
    write_text_meta(Path(__file__), project_root)
    validate(images, preview)

    print(f"Generated {len(COMPONENTS)} components in {output_dir}")
    print(f"Generated atlas: {atlas_path}")
    print(f"Generated preview: {preview_path}")


if __name__ == "__main__":
    main()
