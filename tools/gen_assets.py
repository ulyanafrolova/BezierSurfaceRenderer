#!/usr/bin/env python3
"""Generates the default texture and normal maps shipped in src/BezierSurfaceRenderer.App/Assets.

Standard library only (PNG written with zlib + struct). Run from the repository root:
    python3 tools/gen_assets.py
"""
import math
import os
import random
import struct
import zlib

ASSETS = os.path.join(os.path.dirname(__file__), "..", "src", "BezierSurfaceRenderer.App", "Assets")


def write_png(path, width, height, pixel):
    """pixel(x, y) -> (r, g, b) with ints 0..255."""
    raw = bytearray()
    for y in range(height):
        raw.append(0)  # filter: none
        for x in range(width):
            raw.extend(pixel(x, y))

    def chunk(tag, data):
        body = tag + data
        return struct.pack(">I", len(data)) + body + struct.pack(">I", zlib.crc32(body) & 0xFFFFFFFF)

    png = b"\x89PNG\r\n\x1a\n"
    png += chunk(b"IHDR", struct.pack(">IIBBBBB", width, height, 8, 2, 0, 0, 0))
    png += chunk(b"IDAT", zlib.compress(bytes(raw), 9))
    png += chunk(b"IEND", b"")
    with open(path, "wb") as f:
        f.write(png)
    print("wrote", os.path.relpath(path))


def clamp8(v):
    return max(0, min(255, int(round(v))))


def encode_normal(nx, ny, nz):
    length = math.sqrt(nx * nx + ny * ny + nz * nz)
    nx, ny, nz = nx / length, ny / length, nz / length
    # [-1,1] -> [0,255]; nz > 0 keeps Blue in 128..255 (spec §6).
    return clamp8((nx + 1) * 127.5), clamp8((ny + 1) * 127.5), clamp8((nz + 1) * 127.5)


def normals_from_height(width, height, h, strength):
    """Central differences of a tileable height field -> tangent-space normal pixel function."""
    def pixel(x, y):
        dx = h[y][(x + 1) % width] - h[y][(x - 1) % width]
        dy = h[(y + 1) % height][x] - h[(y - 1) % height][x]
        # Image rows grow downwards while v grows with rows, so tangent "v" follows +row.
        return encode_normal(-dx * strength, -dy * strength, 1.0)
    return pixel


def default_texture():
    """Blueprint-style grid with a cyan→violet gradient: makes the u–v mapping easy to read."""
    size = 512

    def pixel(x, y):
        u, v = x / (size - 1), y / (size - 1)
        r = 30 + 120 * u
        g = 70 + 140 * (1 - u) * (1 - 0.4 * v)
        b = 150 + 90 * v
        major = (x % 64 in (0, 1)) or (y % 64 in (0, 1))
        minor = (x % 16 == 0) or (y % 16 == 0)
        if major:
            r, g, b = r + 90, g + 90, b + 70
        elif minor:
            r, g, b = r + 30, g + 30, b + 25
        return clamp8(r), clamp8(g), clamp8(b)

    write_png(os.path.join(ASSETS, "textures", "default.png"), size, size, pixel)


def brick_normal_map():
    size = 256
    rows, cols, mortar, bevel = 8, 4, 3.0, 5.0
    bh, bw = size / rows, size / cols
    rng = random.Random(7)
    noise = [[rng.uniform(-0.15, 0.15) for _ in range(size)] for _ in range(size)]
    h = []
    for y in range(size):
        row = int(y // bh)
        offset = (bw / 2) if row % 2 else 0.0
        line = []
        for x in range(size):
            lx = (x + offset) % bw
            ly = y % bh
            d = min(lx, bw - lx, ly, bh - ly)  # distance to the brick's border
            if d < mortar:
                height = 0.0
            elif d < mortar + bevel:
                height = (d - mortar) / bevel
            else:
                height = 1.0
            line.append(height * 4.0 + noise[y][x])
        h.append(line)
    write_png(os.path.join(ASSETS, "normalmaps", "bricks.png"), size, size, normals_from_height(size, size, h, 0.6))


def sphere_normal_map():
    size = 256
    radius = size * 0.32
    c = (size - 1) / 2

    def pixel(x, y):
        dx, dy = (x - c) / radius, (y - c) / radius
        d2 = dx * dx + dy * dy
        if d2 >= 1:
            return 127, 127, 255
        return encode_normal(dx, dy, math.sqrt(1 - d2))

    write_png(os.path.join(ASSETS, "normalmaps", "sphere.png"), size, size, pixel)


if __name__ == "__main__":
    os.makedirs(os.path.join(ASSETS, "textures"), exist_ok=True)
    os.makedirs(os.path.join(ASSETS, "normalmaps"), exist_ok=True)
    default_texture()
    brick_normal_map()
    sphere_normal_map()
