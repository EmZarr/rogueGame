import json
import os
import textwrap
import hashlib
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

JSON_FILENAME = "maps.json"          # exported by MapJsonExporter
ICON_DIR = "icons"
BASE_FONT_SIZE = 18
BANNER_MARGIN = 8

# ---------------------------------
# Enum values from Unity
# ---------------------------------
class EnemyType:
    MELEE = 0
    RANGED = 1
    BOMBER = 2
    ASSASSIN = 3
    GUARDIAN = 4


class LootType:
    POWERUP = 0
    HEALTH = 1


class TerrainType:
    STANDARD = 0
    DECOR1 = 1
    DECOR2 = 2
    PATH = 3


class ObstacleType:
    SPIKE = 0
    PILLAR = 1


# ---------------------------------
# Colors (RGB)
# ---------------------------------
COLORS = {
    "empty": (20, 20, 20),
    "ground": (144, 238, 144),
    "path": (180, 180, 80),
    "decor": (110, 190, 110),

    "loot": (0, 80, 255),
    "spike": (255, 0, 0),
    "pillar": (120, 120, 120),

    "enemy_ranged": (255, 255, 0),
    "enemy_melee": (160, 32, 240),
    "enemy_bomber": (255, 140, 0),
    "enemy_assassin": (255, 0, 255),
    "enemy_guardian": (0, 255, 255),

    "player": (255, 255, 255),
    "exit": (0, 0, 0),

    "room_outline": (45, 45, 45),
    "grid": (45, 45, 45),
}

# ---------------------------------
# Helpers
# ---------------------------------
def load_json(path: Path):
    with open(path, "r", encoding="utf-8") as f:
        return json.load(f)


def _v2_xy(v):
    """
    Accepts Unity JsonUtility Vector2Int serialization in either form:
      {"x": 1, "y": 2}
    or
      [1, 2]
    """
    if isinstance(v, dict):
        return int(v.get("x", 0)), int(v.get("y", 0))
    if isinstance(v, (list, tuple)) and len(v) >= 2:
        return int(v[0]), int(v[1])
    return 0, 0


def _iter_room_grid_entries(m: dict, field_name: str):
    for room in m.get("rooms", []) or []:
        for entry in room.get(field_name, []) or []:
            pos = entry.get("pos", {})
            x, y = _v2_xy(pos)
            yield x, y, int(entry.get("type", 0))


def _iter_all_positions(m: dict):
    # room tiles / enemies / loot / obstacles
    for field in ("tiles", "enemies", "loot", "obstacles"):
        for x, y, _ in _iter_room_grid_entries(m, field):
            yield x, y

    # room chunks, entry/exit, room positions
    for room in m.get("rooms", []) or []:
        for chunk in room.get("chunks", []) or []:
            px, py = _v2_xy(chunk.get("position", {}))
            sx, sy = _v2_xy(chunk.get("size", {}))
            for yy in range(py, py + max(0, sy)):
                for xx in range(px, px + max(0, sx)):
                    yield xx, yy

        ex, ey = _v2_xy(room.get("entryTile", {}))
        ox, oy = _v2_xy(room.get("exitTile", {}))
        yield ex, ey
        yield ox, oy

        rx, ry = _v2_xy(room.get("position", {}))
        yield rx, ry

    # connections
    for conn in m.get("connections", []) or []:
        ax, ay = _v2_xy(conn.get("tileA", {}))
        bx, by = _v2_xy(conn.get("tileB", {}))
        yield ax, ay
        yield bx, by


def compute_bounds(m: dict):
    pts = list(_iter_all_positions(m))
    if not pts:
        return 0, 0, -1, -1

    xs = [p[0] for p in pts]
    ys = [p[1] for p in pts]
    return min(xs), min(ys), max(xs), max(ys)


def make_canvas_arrays(m: dict):
    min_x, min_y, max_x, max_y = compute_bounds(m)
    if max_x < min_x or max_y < min_y:
        return None

    w = max_x - min_x + 1
    h = max_y - min_y + 1

    # base terrain layer:
    # -1 = empty
    #  0 = ground
    #  1 = decor
    #  2 = path
    terrain = np.full((h, w), -1, dtype=np.int16)

    # marker layers
    enemies = np.full((h, w), -1, dtype=np.int16)
    loot = np.full((h, w), -1, dtype=np.int16)
    obstacles = np.full((h, w), -1, dtype=np.int16)

    def to_local(x, y):
        return x - min_x, y - min_y

    # Fill chunks first as generic ground if present
    for room in m.get("rooms", []) or []:
        for chunk in room.get("chunks", []) or []:
            px, py = _v2_xy(chunk.get("position", {}))
            sx, sy = _v2_xy(chunk.get("size", {}))
            for yy in range(py, py + max(0, sy)):
                for xx in range(px, px + max(0, sx)):
                    lx, ly = to_local(xx, yy)
                    if 0 <= lx < w and 0 <= ly < h:
                        terrain[ly, lx] = max(terrain[ly, lx], 0)

    # Then apply explicit tiles with terrain types
    for x, y, t in _iter_room_grid_entries(m, "tiles"):
        lx, ly = to_local(x, y)
        if not (0 <= lx < w and 0 <= ly < h):
            continue

        if t == TerrainType.PATH:
            terrain[ly, lx] = 2
        elif t in (TerrainType.DECOR1, TerrainType.DECOR2):
            terrain[ly, lx] = max(terrain[ly, lx], 1)
        else:
            terrain[ly, lx] = max(terrain[ly, lx], 0)

    # Connections can be shown as path endpoints; line interpolation added below
    connection_segments = []
    for conn in m.get("connections", []) or []:
        ax, ay = _v2_xy(conn.get("tileA", {}))
        bx, by = _v2_xy(conn.get("tileB", {}))
        connection_segments.append(((ax, ay), (bx, by)))

    # Overlay entities
    for x, y, t in _iter_room_grid_entries(m, "enemies"):
        lx, ly = to_local(x, y)
        if 0 <= lx < w and 0 <= ly < h:
            enemies[ly, lx] = t
            terrain[ly, lx] = max(terrain[ly, lx], 0)

    for x, y, t in _iter_room_grid_entries(m, "loot"):
        lx, ly = to_local(x, y)
        if 0 <= lx < w and 0 <= ly < h:
            loot[ly, lx] = t
            terrain[ly, lx] = max(terrain[ly, lx], 0)

    for x, y, t in _iter_room_grid_entries(m, "obstacles"):
        lx, ly = to_local(x, y)
        if 0 <= lx < w and 0 <= ly < h:
            obstacles[ly, lx] = t
            terrain[ly, lx] = max(terrain[ly, lx], 0)

    # Approximate corridor visualization by drawing orthogonal lines
    for (ax, ay), (bx, by) in connection_segments:
        x0, y0 = ax, ay
        x1, y1 = bx, by

        # horizontal then vertical
        step_x = 1 if x1 >= x0 else -1
        for xx in range(x0, x1 + step_x, step_x):
            lx, ly = to_local(xx, y0)
            if 0 <= lx < w and 0 <= ly < h:
                terrain[ly, lx] = max(terrain[ly, lx], 2)

        step_y = 1 if y1 >= y0 else -1
        for yy in range(y0, y1 + step_y, step_y):
            lx, ly = to_local(x1, yy)
            if 0 <= lx < w and 0 <= ly < h:
                terrain[ly, lx] = max(terrain[ly, lx], 2)

    # Start / exit markers
    player_pos = None
    exit_pos = None

    start_idx = int(m.get("startRoomIndex", -1))
    end_idx = int(m.get("endRoomIndex", -1))
    rooms = m.get("rooms", []) or []

    if 0 <= start_idx < len(rooms):
        sx, sy = _v2_xy(rooms[start_idx].get("entryTile", {}))
        player_pos = to_local(sx, sy)

    if 0 <= end_idx < len(rooms):
        ex, ey = _v2_xy(rooms[end_idx].get("exitTile", {}))
        exit_pos = to_local(ex, ey)

    return {
        "terrain": terrain,
        "enemies": enemies,
        "loot": loot,
        "obstacles": obstacles,
        "player_pos": player_pos,
        "exit_pos": exit_pos,
        "min_x": min_x,
        "min_y": min_y,
        "width": w,
        "height": h,
    }


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


# =========================================
# Behavior tuple / hash
# =========================================
def behavior_tuple_from_json(m: dict) -> tuple:
    """
    Canonical behavior tuple:
      (geo.x, 0, furn.x, furn.y, enemy.x, enemy.y)

    Supports JsonUtility Vector2Int shape:
      {"x": ..., "y": ...}
    and legacy [x, y].
    """
    def v2_int(name):
        if name not in m:
            raise KeyError(f"Map missing '{name}'.")
        x, y = _v2_xy(m[name])
        return int(x), int(y)

    geo_x, geo_y = v2_int("geoBehavior")
    furn_x, furn_y = v2_int("furnBehavior")
    enem_x, enem_y = v2_int("enemyBehavior")

    geo_y = 0
    return (geo_x, geo_y, furn_x, furn_y, enem_x, enem_y)


def behavior_hash8(t: tuple) -> str:
    s = repr(tuple(t))
    return hashlib.md5(s.encode("utf-8")).hexdigest()[:8]


def out_path_for_map(m: dict) -> str:
    bt = behavior_tuple_from_json(m)
    h = behavior_hash8(bt)
    return os.path.join(ICON_DIR, f"behavior_{h}.png")


def _fmt_vec(v):
    if isinstance(v, dict):
        x, y = _v2_xy(v)
        return f"[{x}, {y}]"
    if isinstance(v, (list, tuple)):
        return "[" + ", ".join(str(x) for x in v) + "]"
    return "[]"


def render_map(canvas_data: dict, dto: dict, out_path: str):
    if canvas_data is None:
        return

    terrain = canvas_data["terrain"]
    enemies = canvas_data["enemies"]
    loot = canvas_data["loot"]
    obstacles = canvas_data["obstacles"]
    player_pos = canvas_data["player_pos"]
    exit_pos = canvas_data["exit_pos"]

    h, w = terrain.shape
    max_dim = max(w, h, 1)
    scale = max(8, min(18, int(900 / max_dim)))
    img_w = w * scale
    img_h = h * scale

    canvas = np.zeros((img_h, img_w, 3), dtype=np.uint8)
    markers = []

    for y in range(h):
        for x in range(w):
            x0, y0 = x * scale, y * scale
            block = slice(y0, y0 + scale), slice(x0, x0 + scale)

            tt = int(terrain[y, x])
            if tt < 0:
                base = COLORS["empty"]
            elif tt == 2:
                base = COLORS["path"]
            elif tt == 1:
                base = COLORS["decor"]
            else:
                base = COLORS["ground"]

            canvas[block] = base

            cx = x0 + scale // 2
            cy = y0 + scale // 2

            ot = int(obstacles[y, x])
            if ot == ObstacleType.SPIKE:
                markers.append((cx, cy, COLORS["spike"]))
            elif ot == ObstacleType.PILLAR:
                markers.append((cx, cy, COLORS["pillar"]))

            lt = int(loot[y, x])
            if lt >= 0:
                markers.append((cx, cy, COLORS["loot"]))

            et = int(enemies[y, x])
            if et == EnemyType.RANGED:
                markers.append((cx, cy, COLORS["enemy_ranged"]))
            elif et == EnemyType.MELEE:
                markers.append((cx, cy, COLORS["enemy_melee"]))
            elif et == EnemyType.BOMBER:
                markers.append((cx, cy, COLORS["enemy_bomber"]))
            elif et == EnemyType.ASSASSIN:
                markers.append((cx, cy, COLORS["enemy_assassin"]))
            elif et == EnemyType.GUARDIAN:
                markers.append((cx, cy, COLORS["enemy_guardian"]))

    img = Image.fromarray(canvas)
    draw = ImageDraw.Draw(img)

    grid_color = COLORS["grid"]
    line_width = max(1, scale // 12)
    for gx in range(w + 1):
        xx = gx * scale
        draw.line([(xx, 0), (xx, img_h)], fill=grid_color, width=line_width)
    for gy in range(h + 1):
        yy = gy * scale
        draw.line([(0, yy), (img_w, yy)], fill=grid_color, width=line_width)

    marker_r = max(3, int(scale * 0.35))
    for cx, cy, color in markers:
        bbox = [cx - marker_r, cy - marker_r, cx + marker_r, cy + marker_r]
        draw.ellipse(bbox, fill=color)

    # start / exit on top
    special_r = max(3, int(scale * 0.42))
    if player_pos is not None:
        px, py = player_pos
        if 0 <= px < w and 0 <= py < h:
            cx = px * scale + scale // 2
            cy = py * scale + scale // 2
            bbox = [cx - special_r, cy - special_r, cx + special_r, cy + special_r]
            draw.ellipse(bbox, fill=COLORS["player"])

    if exit_pos is not None:
        ex, ey = exit_pos
        if 0 <= ex < w and 0 <= ey < h:
            margin = max(2, scale // 5)
            draw.rectangle(
                [
                    ex * scale + margin,
                    ey * scale + margin,
                    (ex + 1) * scale - margin,
                    (ey + 1) * scale - margin,
                ],
                fill=COLORS["exit"],
            )

    rooms = dto.get("rooms", []) or []
    connection_count = len(dto.get("connections", []) or [])
    enemy_count = sum(len(r.get("enemies", []) or []) for r in rooms)
    loot_count = sum(len(r.get("loot", []) or []) for r in rooms)
    obstacle_count = sum(len(r.get("obstacles", []) or []) for r in rooms)
    tile_count = sum(len(r.get("tiles", []) or []) for r in rooms)
    chunk_count = sum(len(r.get("chunks", []) or []) for r in rooms)

    geo_fit = dto.get("geoFitness", 0.0)
    enem_fit = dto.get("enemFitness", dto.get("enemyFitness", 0.0))
    furn_fit = dto.get("furnFitness", 0.0)
    combined_fit = float(geo_fit) + float(enem_fit) + float(furn_fit)

    text_lines = [
        (
            f"fitness={combined_fit:.2f}   "
            f"geoFit={float(geo_fit):.2f} enemFit={float(enem_fit):.2f} furnFit={float(furn_fit):.2f}   "
            f"geo={_fmt_vec(dto.get('geoBehavior'))}   "
            f"enemy={_fmt_vec(dto.get('enemyBehavior'))}   "
            f"furn={_fmt_vec(dto.get('furnBehavior'))}"
        ),
        (
            f"rooms={len(rooms)} connections={connection_count} chunks={chunk_count} "
            f"tiles={tile_count} enemies={enemy_count} loot={loot_count} obstacles={obstacle_count}"
        ),
        (
            f"difficulty={dto.get('difficulty', 0)}   "
            f"bounds=({canvas_data['min_x']},{canvas_data['min_y']}) -> "
            f"({canvas_data['min_x'] + w - 1},{canvas_data['min_y'] + h - 1})"
        ),
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
    maps = data.get("wrappedMaps", [])
    if not maps:
        print("No maps found in JSON (expected key: 'wrappedMaps').")
        return

    os.makedirs(ICON_DIR, exist_ok=True)

    saved = 0
    skipped = 0
    seen = set()

    for i, m in enumerate(maps):
        try:
            bt = behavior_tuple_from_json(m)
        except KeyError as e:
            print(f"[SKIP map index {i}] {e}")
            skipped += 1
            continue

        if bt in seen:
            continue
        seen.add(bt)

        canvas_data = make_canvas_arrays(m)
        if canvas_data is None:
            print(f"[SKIP map index {i}] no renderable geometry found")
            skipped += 1
            continue

        out_file = os.path.join(ICON_DIR, f"behavior_{behavior_hash8(bt)}.png")
        render_map(canvas_data, m, out_file)
        saved += 1
        print(
            f"Saved {out_file} "
            f"(w={canvas_data['width']}, h={canvas_data['height']})"
        )

    print(f"\nDone. Saved={saved}, skipped={skipped}, unique_behaviors={len(seen)}")


if __name__ == "__main__":
    main()