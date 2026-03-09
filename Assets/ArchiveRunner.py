import json
import os
import textwrap
import hashlib
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

JSON_FILENAME = "buildMapsArchive.json"
ICON_DIR = "icons"              # must match the visualizer
BASE_FONT_SIZE = 18
BANNER_MARGIN = 8

# -----------------------------
# Tile ID mapping (Unity -> image)
# -----------------------------
EMPTY_TILE = 0
FLOOR_TILE = 1
ROAD_TILES = {2}
WALL_TILES = {3, 4, 5, 31, 32}
DECOR_TILES = {25, 26, 27}

LOOT_TILES = {13, 14}
SPIKE_TILES = {11, 12}
RANGED_ENEMIES = {40, 41, 42}
MELEE_ENEMIES = {43, 44}
EXIT_TILE = 99
PLAYER_TILE = 100

# -----------------------------
# Colors (RGB)
# -----------------------------
COLORS = {
    "empty": (20, 20, 20),
    "ground": (144, 238, 144),
    "road": (180, 180, 80),
    "wall": (120, 120, 120),
    "decor": (144, 238, 144),

    "loot": (0, 80, 255),
    "spike": (255, 0, 0),
    "enemy_ranged": (255, 255, 0),
    "enemy_melee": (160, 32, 240),
    "player": (255, 255, 255),
    "exit": (0, 0, 0),
}


def load_json(path: Path):
    with open(path, "r", encoding="utf-8") as f:
        return json.load(f)


def reconstruct_array(m: dict) -> np.ndarray:
    w = int(m.get("width", 0))
    h = int(m.get("height", 0))
    if w <= 0 or h <= 0:
        return np.zeros((0, 0), dtype=int)

    if m.get("flatTiles") is not None:
        flat = np.array(m["flatTiles"], dtype=int).reshape(-1)
        if flat.size != w * h:
            size = min(flat.size, w * h)
            flat = flat[:size]
            if size < w * h:
                pad = np.zeros((w * h - size,), dtype=int)
                flat = np.concatenate([flat, pad], axis=0)

        arr = flat.reshape((h, w))
        if np.count_nonzero(arr) == 0:
            return flat.reshape((w, h)).T
        return arr

    if m.get("tiles") is not None:
        arr = np.array(m["tiles"], dtype=int)
        if arr.shape == (h, w):
            return arr
        if arr.shape == (w, h):
            return arr.T
        try:
            return arr.reshape(-1).reshape((h, w))
        except Exception:
            return np.zeros((h, w), dtype=int)

    return np.zeros((h, w), dtype=int)


def _text_size(draw: ImageDraw.ImageDraw, text: str, font: ImageFont.ImageFont):
    try:
        bbox = draw.textbbox((0, 0), text, font=font)
        return bbox[2] - bbox[0], bbox[3] - bbox[1]
    except Exception:
        try:
            return font.getsize(text)
        except Exception:
            approx_w = int(len(text) * (getattr(font, "size", BASE_FONT_SIZE) * 0.6))
            approx_h = getattr(font, "size", BASE_FONT_SIZE)
            return approx_w, approx_h


# ==========================
# NAMING CONVENTION (md5(repr(tuple))[:8])
# ==========================
def behavior_tuple_from_json(m: dict) -> tuple:
    """
    Canonical INT behavior tuple for hashing:
      (GeometryBehavior, geoY, FurnishingBehaviorSpread, FurnishingBehaviorRatio,
       EnemyBehaviorRatio, EnemyBehaviorDifficulty)

    Where:
      GeometryBehavior == geo.x
      geoY is always 0 (matches your Unity code for geometry behavior)
      FurnishingBehaviorSpread == furn.x
      FurnishingBehaviorRatio  == furn.y
      EnemyBehaviorRatio       == enemy.x
      EnemyBehaviorDifficulty  == enemy.y
    """
    def v2_int(name):
        v = m.get(name, None)
        if not isinstance(v, (list, tuple)) or len(v) < 2:
            raise KeyError(f"Map missing '{name}' [x,y].")
        return int(round(float(v[0]))), int(round(float(v[1])))

    geo_x, geo_y = v2_int("geoBehavior")
    furn_x, furn_y = v2_int("furnBehavior")
    enem_x, enem_y = v2_int("enemyBehavior")

    # if geo_y is always 0 in your pipeline, force it:
    geo_y = 0

    return (geo_x, geo_y, furn_x, furn_y, enem_x, enem_y)


def behavior_hash8(t: tuple) -> str:
    s = repr(tuple(t))  # now repr is like '(3, 0, 5, 1, 2, 1)'
    return hashlib.md5(s.encode("utf-8")).hexdigest()[:8]


def out_path_for_map(m: dict) -> str:
    bt = behavior_tuple_from_json(m)
    h = behavior_hash8(bt)
    return os.path.join(ICON_DIR, f"behavior_{h}.png")


def render_map(arr: np.ndarray, dto: dict, out_path: str):
    if arr.size == 0:
        return

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

            if t == EMPTY_TILE:
                base = COLORS["empty"]
            elif t in WALL_TILES:
                base = COLORS["wall"]
            elif t in ROAD_TILES:
                base = COLORS["road"]
            elif t in DECOR_TILES:
                base = COLORS["decor"]
            else:
                base = COLORS["ground"]
            canvas[block] = base

            cx = x0 + scale // 2
            cy = y0 + scale // 2
            if t in LOOT_TILES:
                markers.append((cx, cy, COLORS["loot"]))
            elif t in SPIKE_TILES:
                markers.append((cx, cy, COLORS["spike"]))
            elif t in RANGED_ENEMIES:
                markers.append((cx, cy, COLORS["enemy_ranged"]))
            elif t in MELEE_ENEMIES:
                markers.append((cx, cy, COLORS["enemy_melee"]))
            elif t == EXIT_TILE:
                markers.append((cx, cy, COLORS["exit"]))
            elif t == PLAYER_TILE:
                markers.append((cx, cy, COLORS["player"]))

    img = Image.fromarray(canvas)
    draw = ImageDraw.Draw(img)

    grid_color = (45, 45, 45)
    line_width = max(1, scale // 12)
    for gx in range(w + 1):
        xx = gx * scale
        draw.line([(xx, 0), (xx, img_h)], fill=grid_color, width=line_width)
    for gy in range(h + 1):
        yy = gy * scale
        draw.line([(0, yy), (img_w, yy)], fill=grid_color, width=line_width)

    marker_r = max(3, int(scale * 0.45))
    for cx, cy, color in markers:
        bbox = [cx - marker_r, cy - marker_r, cx + marker_r, cy + marker_r]
        draw.ellipse(bbox, fill=color)

    def _fmt_vec(v):
        if not v or len(v) == 0:
            return "[]"
        return "[" + ", ".join(f"{x:.3f}" for x in v) + "]"

    geo_b = dto.get("geoBehavior", [])
    enemy_b = dto.get("enemyBehavior", [])
    furn_b = dto.get("furnBehavior", [])

    geo_fit = dto.get("geoFitness", None)
    enemy_fit = dto.get("enemyFitness", None)
    furn_fit = dto.get("furnFitness", None)

    fit_str = f"fitness={dto.get('fitness', 0):.2f}"
    if (geo_fit is not None) and (enemy_fit is not None) and (furn_fit is not None):
        try:
            fit_str += f"  geoFit={float(geo_fit):.2f} enemyFit={float(enemy_fit):.2f} furnFit={float(furn_fit):.2f}"
        except Exception:
            pass

    text_lines = [
        f"{fit_str}   geo={_fmt_vec(geo_b)}   enemy={_fmt_vec(enemy_b)}   furn={_fmt_vec(furn_b)}",
        f"rooms={dto.get('roomsCount',0)} enemies={dto.get('enemiesCount',0)} furnishing={dto.get('furnishingCount',0)}",
        f"walkable={dto.get('walkableTiles',0)} walls={dto.get('wallTiles',0)} enemyBudget={dto.get('enemyBudget',0)} furnishingBudget={dto.get('furnishingBudget',0)}",
    ]
    full_text = "   ".join(text_lines)

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

    if text_width > max_text_width:
        sample = "abcdefghijklmnopqrstuvwxyz"
        sample_w, _ = _text_size(draw, sample, font)
        avg_char_w = (sample_w / len(sample)) if sample_w > 0 else max(1, font_size * 0.6)
        max_chars = max(20, int(max_text_width / avg_char_w))
        wrapped = textwrap.wrap(full_text, width=max_chars)
    else:
        wrapped = [full_text]

    _, line_h = _text_size(draw, "Ay", font)
    line_height = int(line_h * 1.15)
    banner_h = BANNER_MARGIN * 2 + line_height * len(wrapped)

    draw.rectangle([0, 0, img_w, banner_h], fill=(255, 255, 255))
    y_text = BANNER_MARGIN
    for line in wrapped:
        draw.text((BANNER_MARGIN, y_text), line, fill=(0, 0, 0), font=font)
        y_text += line_height

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

    os.makedirs(ICON_DIR, exist_ok=True)

    saved = 0
    skipped = 0
    seen = set()

    for i, m in enumerate(maps):
        arr = reconstruct_array(m)

        try:
            bt = behavior_tuple_from_json(m)
        except KeyError as e:
            print(f"[SKIP map index {i}] {e}")
            skipped += 1
            continue

        # One icon per unique behavior tuple (optional but usually what you want)
        if bt in seen:
            continue
        seen.add(bt)

        out_file = os.path.join(ICON_DIR, f"behavior_{behavior_hash8(bt)}.png")
        render_map(arr, m, out_file)
        saved += 1
        print(f"Saved {out_file} (w={m.get('width')}, h={m.get('height')})")

    print(f"\nDone. Saved={saved}, skipped={skipped}, unique_behaviors={len(seen)}")


if __name__ == "__main__":
    main()