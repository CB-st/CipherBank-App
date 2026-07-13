#!/usr/bin/env python3
"""Generate CipherBank launcher / in-app mark PNGs (centered diamonds)."""
from __future__ import annotations

import math
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / "assets"
LOGO = ASSETS / "logo"

BG_TOP = (0x2B, 0x1E, 0x3E, 255)
BG_BOT = (0x11, 0x13, 0x18, 255)
GOLD = (0xF2, 0xC1, 0x4E, 255)
PURPLE = (0x7B, 0x4D, 0xFF, 255)


def lerp(a: int, b: int, t: float) -> int:
    return int(a + (b - a) * t)


def gradient_bg(size: int) -> Image.Image:
    img = Image.new("RGBA", (size, size))
    px = img.load()
    for y in range(size):
        t = y / max(size - 1, 1)
        c = (
            lerp(BG_TOP[0], BG_BOT[0], t),
            lerp(BG_TOP[1], BG_BOT[1], t),
            lerp(BG_TOP[2], BG_BOT[2], t),
            255,
        )
        for x in range(size):
            px[x, y] = c
    return img


def rounded_mask(size: int, radius: int) -> Image.Image:
    mask = Image.new("L", (size, size), 0)
    ImageDraw.Draw(mask).rounded_rectangle((0, 0, size - 1, size - 1), radius=radius, fill=255)
    return mask


def diamond_points(cx: float, cy: float, half: float) -> list[tuple[float, float]]:
    # Axis-aligned square rotated 45° → diamond
    return [
        (cx, cy - half),
        (cx + half, cy),
        (cx, cy + half),
        (cx - half, cy),
    ]


def draw_mark(draw: ImageDraw.ImageDraw, cx: float, cy: float, scale: float, outline_only: bool = False) -> None:
    # Matches cipherbank-app-icon.svg proportions (outer ~76/180, inner ~38/180 of canvas)
    outer_half = 38 * scale  # half-diagonal of outer diamond
    inner_half = 19 * scale
    stroke = max(2, int(7 * scale))

    outer = diamond_points(cx, cy, outer_half)
    # Stroke as polygon ring approximation: draw thick outline via two polygons
    # Pillow doesn't stroke rotated rects well — draw outline with line+width on closed shape
    draw.line(outer + [outer[0]], fill=GOLD, width=stroke, joint="curve")
    if not outline_only:
        draw.polygon(diamond_points(cx, cy, inner_half), fill=PURPLE)


def make_app_icon(size: int = 1024) -> Image.Image:
    base = gradient_bg(size)
    mask = rounded_mask(size, radius=int(size * 40 / 180))
    out = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    out.paste(base, (0, 0), mask)
    draw = ImageDraw.Draw(out)
    scale = size / 180
    draw_mark(draw, size / 2, size / 2, scale)
    return out


def make_adaptive_foreground(size: int = 1024) -> Image.Image:
    """Transparent plate; mark in Android adaptive safe zone (~66% center)."""
    out = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(out)
    # Safe zone diameter ~66% of canvas
    scale = (size * 0.52) / 76  # outer diamond width in svg units ≈ 76*√2/2 wait — use half-diag 38
    draw_mark(draw, size / 2, size / 2, scale)
    return out


def make_in_app_mark(size: int = 180) -> Image.Image:
    out = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(out)
    scale = size / 180
    draw_mark(draw, size / 2, size / 2, scale)
    return out


def make_splash_icon(size: int = 512) -> Image.Image:
    return make_app_icon(size)


def main() -> None:
    ASSETS.mkdir(parents=True, exist_ok=True)
    LOGO.mkdir(parents=True, exist_ok=True)

    make_app_icon(1024).save(ASSETS / "icon.png", "PNG")
    make_adaptive_foreground(1024).save(ASSETS / "adaptive-icon.png", "PNG")
    make_splash_icon(512).save(ASSETS / "splash.png", "PNG")
    make_app_icon(192).save(ASSETS / "favicon.png", "PNG")
    make_app_icon(180).save(LOGO / "cipherbank-app-icon.png", "PNG")
    make_in_app_mark(180).save(LOGO / "cipherbank-mark.png", "PNG")

    print("Wrote brand icons under", ASSETS)


if __name__ == "__main__":
    main()
