import json
import os
import textwrap
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

JSON_FILENAME = "combArchive_maps.json"

# One PNG sheet: 5 maps across, 50 rows down (250 maps per sheet)
OUT_SHEET_PREFIX = "sheet_"
SHEET_COLS = 10
SHEET_ROWS = 5
MAPS_PER_SHEET = SHEET_COLS * SHEET_ROWS  # 250

BASE_FONT_SIZE = 18
BANNER_MARGIN = 8

SHEET_PADDING = 20
SHEET_BG_COLOR = (30, 30, 30)

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
        if arr.shape == (h, w):
            return arr
        if arr.shape == (w, h):
            return arr.T
        return arr.reshape((h, w))
    return np.zeros((h, w), dtype=int)


def _text_size(draw, text, font):
    """Robust text size measurement across Pillow versions. Returns (w,h)."""
    try:
        bbox = draw.textbbox((0, 0), text, font=font)
        return (bbox[2] - bbox[0], bbox[3] - bbox[1])
    except Exception:
        try:
            return font.getsize(text)
        except Exception:
            approx_w = int(len(text) * (getattr(font, "size", BASE_FONT_SIZE) * 0.6))
            approx_h = getattr(font, "size", BASE_FONT_SIZE)
            return approx_w, approx_h


def render_map_image(arr, dto) -> Image.Image:
    """Render ONE map as a PIL Image and RETURN it (does not save)."""
    h, w = arr.shape

    max_dim = max(w, h, 1)
    scale = max(8, min(18, int(900 / max_dim)))
    img_w = w * scale
    img_h = h * scale

    canvas = np.zeros((img_h, img_w, 3), dtype=np.uint8)
    markers = []

    for y in range(h):
        for x in range(w):
            t = int(arr[y, x])
            x0, y0 = x * scale, y * scale
            block = slice(y0, y0 + scale), slice(x0, x0 + scale)

            if t == 0:
                base = COLORS["empty"]
            elif t == 2:
                base = COLORS["wall"]
            else:
                base = COLORS["ground"]
            canvas[block] = base

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

    # grid lines
    grid_color = (45, 45, 45)
    line_width = max(1, scale // 12)
    for gx in range(w + 1):
        x = gx * scale
        draw.line([(x, 0), (x, img_h)], fill=grid_color, width=line_width)
    for gy in range(h + 1):
        y = gy * scale
        draw.line([(0, y), (img_w, y)], fill=grid_color, width=line_width)

    # markers
    marker_r = max(3, int(scale * 0.45))
    for cx, cy, color in markers:
        bbox = [cx - marker_r, cy - marker_r, cx + marker_r, cy + marker_r]
        draw.ellipse(bbox, fill=color)

    # metadata text
    def _fmt_vec2(v):
        if not v or len(v) < 2:
            return "[]"
        return f"[{v[0]:.3f}, {v[1]:.3f}]"

    geo_b = dto.get("geoBehavior", [])
    enemy_b = dto.get("enemyBehavior", [])
    furn_b = dto.get("furnBehavior", [])

    combined_b = []
    if len(geo_b) >= 2: combined_b += geo_b[:2]
    if len(enemy_b) >= 2: combined_b += enemy_b[:2]
    if len(furn_b) >= 2: combined_b += furn_b[:2]

    text_lines = [
        f"fitness={dto.get('fitness', 0):.2f}  geo={_fmt_vec2(geo_b)}  enemy={_fmt_vec2(enemy_b)}  furn={_fmt_vec2(furn_b)}",
        f"behavior6={['{:.3f}'.format(x) for x in combined_b]}",
        f"rooms={dto.get('roomsCount',0)} enemies={dto.get('enemiesCount',0)} furnishing={dto.get('furnishingCount',0)}",
        f"walkable={dto.get('walkableTiles',0)} walls={dto.get('wallTiles',0)} enemyBudget={dto.get('enemyBudget',0)} furnishingBudget={dto.get('furnishingBudget',0)}",
    ]
    full_text = "   ".join(text_lines)

    # font
    font_size = BASE_FONT_SIZE
    try:
        font = ImageFont.truetype("arial.ttf", font_size)
    except Exception:
        font = ImageFont.load_default()

    max_text_width = img_w - BANNER_MARGIN * 2
    text_width, _ = _text_size(draw, full_text, font)
    while text_width > max_text_width and font_size > 8:
        font_size -= 1
        try:
            font = ImageFont.truetype("arial.ttf", font_size)
        except Exception:
            font = ImageFont.load_default()
        text_width, _ = _text_size(draw, full_text, font)

    # wrap if needed
    if text_width > max_text_width:
        sample = "abcdefghijklmnopqrstuvwxyz"
        sample_w, _ = _text_size(draw, sample, font)
        avg_char_w = (sample_w / len(sample)) if sample_w > 0 else max(1, font_size * 0.6)
        max_chars = max(20, int(max_text_width / avg_char_w))
        wrapped = textwrap.wrap(full_text, width=max_chars)
    else:
        wrapped = [full_text]

    # banner
    _, line_h = _text_size(draw, "Ay", font)
    line_height = int(line_h * 1.15)
    banner_h = BANNER_MARGIN * 2 + line_height * len(wrapped)

    draw.rectangle([0, 0, img_w, banner_h], fill=(255, 255, 255))
    y_text = BANNER_MARGIN
    for line in wrapped:
        draw.text((BANNER_MARGIN, y_text), line, fill=(0, 0, 0), font=font)
        y_text += line_height

    return img


def make_sheet(map_images, cols=SHEET_COLS, rows=SHEET_ROWS, padding=SHEET_PADDING) -> Image.Image:
    """
    Pack up to cols*rows images into one sheet.
    Uses only the number of rows needed for the provided images (up to 'rows'),
    so the last sheet doesn't end up huge and mostly empty.
    """
    n = len(map_images)
    if n == 0:
        return Image.new("RGB", (200, 200), SHEET_BG_COLOR)

    capacity = cols * rows
    n = min(n, capacity)

    used_rows = (n + cols - 1) // cols  # ceil(n/cols)
    used_rows = min(used_rows, rows)

    col_w = [0] * cols
    row_h = [0] * used_rows

    for idx in range(n):
        im = map_images[idx]
        r = idx // cols
        c = idx % cols
        col_w[c] = max(col_w[c], im.width)
        row_h[r] = max(row_h[r], im.height)

    sheet_w = padding + sum(col_w) + padding * (cols - 1) + padding
    sheet_h = padding + sum(row_h) + padding * (used_rows - 1) + padding

    sheet = Image.new("RGB", (sheet_w, sheet_h), SHEET_BG_COLOR)

    y = padding
    idx = 0
    for r in range(used_rows):
        x = padding
        for c in range(cols):
            if idx < n:
                im = map_images[idx]
                x_off = x + (col_w[c] - im.width) // 2
                y_off = y + (row_h[r] - im.height) // 2
                sheet.paste(im, (x_off, y_off))
            idx += 1
            x += col_w[c] + padding
        y += row_h[r] + padding

    return sheet


def chunked(seq, size):
    for i in range(0, len(seq), size):
        yield i, seq[i:i + size]


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

    for sheet_i, (start_index, maps_chunk) in enumerate(chunked(maps, MAPS_PER_SHEET)):
        rendered = []
        for m in maps_chunk:
            arr = reconstruct_array(m)
            if arr.shape[0] != int(m.get("height", arr.shape[0])):
                try:
                    arr = arr.T
                except Exception:
                    pass
            rendered.append(render_map_image(arr, m))

        sheet = make_sheet(rendered, cols=SHEET_COLS, rows=SHEET_ROWS)
        out_file = f"{OUT_SHEET_PREFIX}{sheet_i}.png"
        sheet.save(out_file)
        print(f"Saved {out_file} (maps {start_index}..{start_index + len(maps_chunk) - 1})")


if __name__ == "__main__":
    main()
