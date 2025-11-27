import json
import os
import textwrap
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

JSON_FILENAME = "archive_maps.json"
OUT_IMG_PREFIX = "map_"
BASE_FONT_SIZE = 18
BANNER_MARGIN = 8

# Tile color definitions (RGB)
COLORS = {
    "empty": (20, 20, 20),        # 0
    "ground": (144, 238, 144),    # 1 and ground-based tiles
    "wall": (120, 120, 120),      # 2
    "furn0": (160, 82, 45),       # 3
    "furn1": (205, 133, 63),      # 4
    "furn2": (222, 184, 135),     # 5
    "enemy0": (255, 0, 0),        # 6
    "enemy1": (139, 0, 0),        # 7
    "exit": (255, 255, 255),      # 8
    "player": (0, 100, 255)       # 100
}


def load_json(path):
    with open(path, "r", encoding="utf-8") as f:
        return json.load(f)


def reconstruct_array(m):
    w = int(m.get("width", 0))
    h = int(m.get("height", 0))
    if m.get("flatTiles"):
        flat = np.array(m["flatTiles"], dtype=int)
        try:
            return flat.reshape((h, w))
        except Exception:
            return flat.reshape((w, h)).T
    if m.get("tiles"):
        arr = np.array(m["tiles"], dtype=int)
        # ensure shape is (h,w)
        if arr.shape == (h, w):
            return arr
        if arr.shape == (w, h):
            return arr.T
        return arr.reshape((h, w))
    return np.zeros((h, w), dtype=int)


def _text_size(draw, text, font):
    """
    Robust text size measurement that works across Pillow versions.
    Returns (width, height).
    """
    try:
        # Pillow >= 8.0
        bbox = draw.textbbox((0, 0), text, font=font)
        w = bbox[2] - bbox[0]
        h = bbox[3] - bbox[1]
        return w, h
    except Exception:
        try:
            # Older Pillow or fallback
            return font.getsize(text)
        except Exception:
            # Last resort: estimate
            approx_w = int(len(text) * (getattr(font, "size", BASE_FONT_SIZE) * 0.6))
            approx_h = getattr(font, "size", BASE_FONT_SIZE)
            return approx_w, approx_h


def render_map(arr, dto, out_path):
    h, w = arr.shape
    # choose scale so tiles are visible but image not insanely large
    max_dim = max(w, h, 1)
    scale = max(8, min(18, int(900 / max_dim)))  # tweak bounds here
    img_w = w * scale
    img_h = h * scale

    # canvas base
    canvas = np.zeros((img_h, img_w, 3), dtype=np.uint8)

    markers = []  # (cx, cy, color)

    for y in range(h):
        for x in range(w):
            t = int(arr[y, x])  # arr is (h,w) with row=y, col=x
            x0, y0 = x * scale, y * scale
            block = slice(y0, y0 + scale), slice(x0, x0 + scale)

            # base color
            if t == 0:
                base = COLORS["empty"]
            elif t == 2:
                base = COLORS["wall"]
            else:
                base = COLORS["ground"]
            canvas[block] = base

            # collect markers (centers) and colors (draw larger later)
            cx = x0 + scale // 2
            cy = y0 + scale // 2
            if t == 3:
                markers.append((cx, cy, COLORS["furn0"]))
            elif t == 4:
                markers.append((cx, cy, COLORS["furn1"]))
            elif t == 5:
                markers.append((cx, cy, COLORS["furn2"]))
            elif t == 6:
                markers.append((cx, cy, COLORS["enemy0"]))
            elif t == 7:
                markers.append((cx, cy, COLORS["enemy1"]))
            elif t == 99:
                markers.append((cx, cy, COLORS["exit"]))
            elif t == 100:
                markers.append((cx, cy, COLORS["player"]))

    img = Image.fromarray(canvas)
    draw = ImageDraw.Draw(img)

    # draw grid lines to show tile boundaries
    grid_color = (45, 45, 45)  # dark gray grid
    line_width = max(1, scale // 12)
    # vertical lines
    for gx in range(w + 1):
        x = gx * scale
        draw.line([(x, 0), (x, img_h)], fill=grid_color, width=line_width)
    # horizontal lines
    for gy in range(h + 1):
        y = gy * scale
        draw.line([(0, y), (img_w, y)], fill=grid_color, width=line_width)

    # draw markers as filled circles (radius scales with tile size)
    marker_r = max(3, int(scale * 0.45))  # visible markers
    for cx, cy, color in markers:
        bbox = [cx - marker_r, cy - marker_r, cx + marker_r, cy + marker_r]
        draw.ellipse(bbox, fill=color)

    # build metadata text (single long string, then wrap to banner)
    behavior = dto.get("behavior", [])
    text_lines = [
        f"fitness={dto.get('fitness', 0):.2f} behavior={behavior}",
        f"rooms={dto.get('roomsCount',0)} enemies={dto.get('enemiesCount',0)} furnishing={dto.get('furnishingCount',0)}",
        f"walkable={dto.get('walkableTiles',0)} walls={dto.get('wallTiles',0)} budget={dto.get('budget',0)} furnishingBudget={dto.get('furnishingBudget',0)}"
    ]
    full_text = "   ".join(text_lines)

    # load font and adapt size to fit image width, wrap if needed
    font_size = BASE_FONT_SIZE
    try:
        font = ImageFont.truetype("arial.ttf", font_size)
    except Exception:
        font = ImageFont.load_default()

    # measure and reduce font until fits or reaches minimum
    max_text_width = img_w - BANNER_MARGIN * 2
    text_width, _ = _text_size(draw, full_text, font)
    while text_width > max_text_width and font_size > 8:
        font_size -= 1
        try:
            font = ImageFont.truetype("arial.ttf", font_size)
        except Exception:
            font = ImageFont.load_default()
        text_width, _ = _text_size(draw, full_text, font)

    # wrap text to multiple lines if still too wide
    if text_width > max_text_width:
        # estimate max chars per line using average char width
        sample = "abcdefghijklmnopqrstuvwxyz"
        sample_w, _ = _text_size(draw, sample, font)
        avg_char_w = (sample_w / len(sample)) if sample_w > 0 else max(1, font_size * 0.6)
        max_chars = max(20, int(max_text_width / avg_char_w))
        wrapped = textwrap.wrap(full_text, width=max_chars)
    else:
        wrapped = [full_text]

    # compute banner height based on number of lines
    _, line_h = _text_size(draw, "Ay", font)
    line_height = int(line_h * 1.15)
    banner_h = BANNER_MARGIN * 2 + line_height * len(wrapped)

    # draw white banner and text (banner drawn on top of map)
    draw.rectangle([0, 0, img_w, banner_h], fill=(255, 255, 255))
    y_text = BANNER_MARGIN
    for line in wrapped:
        draw.text((BANNER_MARGIN, y_text), line, fill=(0, 0, 0), font=font)
        y_text += line_height

    # save image
    img.save(out_path)


def main():
    p = Path(JSON_FILENAME)
    if not p.exists():
        print(f"{JSON_FILENAME} not found in {os.getcwd()} — put the file here and re-run.")
        return

    data = load_json(p)
    maps = data.get("maps", [])
    if not maps:
        print("No maps found in JSON.")
        return

    for i, m in enumerate(maps):
        arr = reconstruct_array(m)
        # ensure arr shape is (h,w) where h = height
        if arr.shape[0] != int(m.get("height", arr.shape[0])):
            try:
                arr = arr.T
            except Exception:
                pass

        out_file = f"{OUT_IMG_PREFIX}{i}.png"
        render_map(arr, m, out_file)
        print(f"Saved {out_file} (w={m.get('width')}, h={m.get('height')})")


if __name__ == "__main__":
    main()