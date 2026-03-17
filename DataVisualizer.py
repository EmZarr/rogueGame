import os
import sys
import hashlib
import subprocess
from pathlib import Path

import numpy as np
import matplotlib.pyplot as plt
import matplotlib.image as mpimg

from ClusterProject import cluster_entries
from FilteredFeatures import CSV_PATH, INCLUDE_PLAYER_IDS


# ============================================================
# VISUALIZER CONFIG
# Change visual-only settings here.
# ============================================================

ICON_DIR = "icons"
VIDEO_ROOT_DIR = "MyRecordings"
PANEL_WIDTH = 0.33

MARKERS = ["o", "s", "^", "D", "P", "X", "*", "v", "<", ">"]
VIDEO_EXTS = {".mp4", ".mov", ".mkv", ".avi", ".webm", ".m4v"}

BEHAVIOR_FIELDS = [
    ("Room Amount Tier", 0, 100),
    ("Loot on Main Ratio", 2, 5),
    ("Loot Health Ratio", 3, 5),
    ("Enemy Encounter Type", 4, 126),
    ("Difficulty", 5, 5),
]


def session_to_color(session_id: str):
    hash_val = int(hashlib.md5(str(session_id).encode("utf-8")).hexdigest(), 16)
    rng = np.random.RandomState(hash_val % (2**32))
    return tuple(rng.rand(3))


def behavior_hash8(behavior_tuple) -> str:
    s = repr(tuple(behavior_tuple))
    return hashlib.md5(s.encode("utf-8")).hexdigest()[:8]


def behavior_to_icon_path(behavior_tuple, icon_dir: str) -> str | None:
    h = behavior_hash8(behavior_tuple)
    for ext in (".png", ".jpg", ".jpeg", ".webp"):
        p = os.path.join(icon_dir, f"behavior_{h}{ext}")
        if os.path.exists(p):
            return p
    return None


def format_behavior_rows(behavior_tuple: tuple) -> str:
    lines = []
    for name, idx, mx in BEHAVIOR_FIELDS:
        val = int(behavior_tuple[idx])
        lines.append(f"{name}: {val}/{mx}")
    return "\n".join(lines)


def open_expanded(image_path: str, title: str):
    img = mpimg.imread(image_path)
    fig = plt.figure(figsize=(7, 7))
    ax = fig.add_subplot(111)
    ax.imshow(img)
    ax.set_title(title)
    ax.axis("off")
    fig.tight_layout()
    fig.show()


def canonical_behavior_tuple_from_info(info: dict) -> tuple:
    b = info.get("behavior5", [])
    if len(b) != 5:
        raise ValueError(f"Expected behavior5 to contain 5 values, got: {b}")

    return (
        int(b[0]),
        0,
        int(b[1]),
        int(b[2]),
        int(b[3]),
        int(b[4]),
    )


def build_video_match_tokens(entry: dict) -> dict:
    info = entry["info"]

    player_id = str(info["playerId"])
    behavior = canonical_behavior_tuple_from_info(info)
    beh_hash = behavior_hash8(behavior)

    level_play_id = str(info.get("levelPlayID", "")).strip()

    level_tokens = []
    if level_play_id:
        level_tokens = [
            level_play_id,
            level_play_id.replace("-", "_"),
            level_play_id.replace("_", "-"),
            level_play_id.replace(" ", "_"),
            level_play_id.replace(" ", "-"),
        ]

    return {
        "player_id": player_id,
        "behavior_hash": beh_hash,
        "level_play_id": level_play_id,
        "level_tokens": level_tokens,
    }


def iter_video_files(root_dir: str):
    root = Path(root_dir)
    if not root.exists():
        return

    for p in root.rglob("*"):
        if p.is_file() and p.suffix.lower() in VIDEO_EXTS:
            yield p


def find_video_for_entry(entry: dict, video_root_dir: str) -> str | None:
    tokens = build_video_match_tokens(entry)

    player_id = tokens["player_id"]
    behavior_hash = tokens["behavior_hash"]
    level_play_id = tokens["level_play_id"]
    level_tokens = tokens["level_tokens"]

    candidates = []

    for p in iter_video_files(video_root_dir) or []:
        name = p.name

        if player_id not in name:
            continue
        if behavior_hash not in name:
            continue

        score = 0

        if level_play_id and level_play_id in name:
            score += 4
        elif any(tok in name for tok in level_tokens):
            score += 3

        if level_play_id and score < 3:
            continue

        candidates.append((score, p.stat().st_mtime, p))

    if not candidates:
        return None

    candidates.sort(key=lambda x: (x[0], x[1]), reverse=True)
    return str(candidates[0][2])


def open_video_file(video_path: str):
    if not video_path or not os.path.exists(video_path):
        print(f"Video not found: {video_path}")
        return

    try:
        if sys.platform.startswith("win"):
            os.startfile(video_path)  # type: ignore[attr-defined]
        elif sys.platform == "darwin":
            subprocess.Popen(["open", video_path])
        else:
            subprocess.Popen(["xdg-open", video_path])
    except Exception as e:
        print(f"Failed to open video: {e}")


def open_session_popup(entry: dict, icon_path: str | None, video_path: str | None):
    info = entry["info"]
    behavior = canonical_behavior_tuple_from_info(info)
    beh_hash = behavior_hash8(behavior)

    fig = plt.figure(figsize=(9, 7))
    ax = fig.add_subplot(111)
    ax.axis("off")

    if icon_path and os.path.exists(icon_path):
        try:
            img = mpimg.imread(icon_path)
            ax.imshow(img, extent=[0.04, 0.48, 0.35, 0.95], aspect="auto")
        except Exception as e:
            print(f"Could not load icon: {e}")

    text_lines = [
        f"Player: {info.get('playerId', 'unknown')}",
        f"Behavior tuple: {behavior}",
        f"Behavior hash: {beh_hash}",
        f"levelPlayID: {info.get('levelPlayID', 'unknown')}",
        f"Cluster: {info.get('cluster_label', 'unknown')}",
        f"PCA coordinates: {info.get('plot_coordinates', ['?', '?'])}",
        "",
        f"Video found: {'YES' if video_path else 'NO'}",
        f"Video path: {video_path if video_path else '(no matching file found)'}",
    ]

    ax.text(
        0.54,
        0.93,
        "\n".join(text_lines),
        transform=ax.transAxes,
        va="top",
        fontsize=10,
        bbox=dict(boxstyle="round,pad=0.4", facecolor="white", edgecolor="gray"),
    )

    fig.tight_layout()
    fig.show()


def _build_pc_text(projector, feature_names: list[str], pc_index: int, top_n: int = 4) -> str:
    if not hasattr(projector, "components_"):
        return "(projection method has no component loadings)"

    component = projector.components_[pc_index]
    pairs = list(zip(feature_names, component))
    pairs.sort(key=lambda x: abs(x[1]), reverse=True)

    lines = []
    for feature_name, weight in pairs[:top_n]:
        sign = "+" if weight >= 0 else "-"
        lines.append(f"{sign} {feature_name}")
    return "\n".join(lines)


def visualize_player_clusters():
    clustered_entries, projection_model = cluster_entries(return_projection_model=True)

    if not clustered_entries:
        raise ValueError("No entries available after filtering / feature building.")

    feature_names = clustered_entries[0]["feature_names"]

    coords = np.array(
        [entry["info"]["plot_coordinates"] for entry in clustered_entries],
        dtype=float
    )

    behaviors = [canonical_behavior_tuple_from_info(entry["info"]) for entry in clustered_entries]
    unique_behaviors = list(dict.fromkeys(behaviors))

    unique_players = list(dict.fromkeys(entry["info"]["playerId"] for entry in clustered_entries))
    session_color_map = {player_id: session_to_color(player_id) for player_id in unique_players}
    behavior_marker_map = {b: MARKERS[i % len(MARKERS)] for i, b in enumerate(unique_behaviors)}

    explained_variance = getattr(projection_model, "explained_variance_ratio_", None)

    fig = plt.figure(figsize=(16, 10))

    ax = fig.add_axes([0.07, 0.10, 0.88 - PANEL_WIDTH, 0.83])
    panel = fig.add_axes([0.07 + (0.88 - PANEL_WIDTH) + 0.02, 0.10, PANEL_WIDTH - 0.04, 0.83])
    panel.set_axis_off()

    point_targets = {}
    click_targets = {}
    missing_icons = []

    for i, entry in enumerate(clustered_entries):
        info = entry["info"]
        player_id = info["playerId"]
        behavior = canonical_behavior_tuple_from_info(info)

        scatter = ax.scatter(
            coords[i, 0],
            coords[i, 1],
            c=[session_color_map[player_id]],
            marker=behavior_marker_map[behavior],
            s=55,
            alpha=0.75,
            edgecolors="black",
            linewidth=0.3,
            picker=True,
        )
        point_targets[scatter] = i

    center_coords = []
    seen_clusters = set()

    for entry in clustered_entries:
        info = entry["info"]
        cluster_label = info.get("cluster_label")
        center = info.get("cluster_center_coordinates")

        if cluster_label is None or center is None:
            continue

        if cluster_label in seen_clusters:
            continue

        seen_clusters.add(cluster_label)
        center_coords.append(center)

    if center_coords:
        center_coords = np.array(center_coords, dtype=float)
        ax.scatter(
            center_coords[:, 0],
            center_coords[:, 1],
            c="black",
            s=220,
            marker="X",
            label="Cluster Centers",
        )

    if explained_variance is not None and len(explained_variance) >= 2:
        ax.set_xlabel(f"PC1 ({explained_variance[0] * 100:.1f}%)")
        ax.set_ylabel(f"PC2 ({explained_variance[1] * 100:.1f}%)")
    else:
        ax.set_xlabel("Axis 1")
        ax.set_ylabel("Axis 2")

    ax.set_title(
        "Telemetry Behaviour Clustering (2D PCA Projection)\n"
        "Color = Player | Shape = Behavior Configuration | Click a point to open replay"
    )
    ax.grid(True, alpha=0.25)

    panel.text(
        0.02,
        0.98,
        "Behaviors (click to see map)",
        transform=panel.transAxes,
        va="top",
        fontsize=11,
        weight="bold",
    )

    col1_x_marker, col1_x_text = 0.02, 0.08
    col2_x_marker, col2_x_text = 0.52, 0.58

    row_h_beh = 0.17
    top_beh = 0.93
    behaviors_bottom_cutoff = 0.44

    for i, behavior in enumerate(unique_behaviors):
        col = i % 2
        row = i // 2

        x_marker = col1_x_marker if col == 0 else col2_x_marker
        x_text = col1_x_text if col == 0 else col2_x_text

        y = top_beh - row * row_h_beh
        if y < behaviors_bottom_cutoff:
            break

        panel.scatter(
            x_marker,
            y - 0.01,
            transform=panel.transAxes,
            s=70,
            marker=behavior_marker_map[behavior],
            color="lightgray",
            edgecolors="black",
            linewidths=0.6,
            zorder=3,
        )

        icon_path = behavior_to_icon_path(behavior, ICON_DIR)

        label = format_behavior_rows(behavior)
        if icon_path is None:
            missing_icons.append(behavior)
            label += "\n(no icon)"

        txt = panel.text(
            x_text,
            y,
            label,
            transform=panel.transAxes,
            va="top",
            fontsize=9,
            linespacing=1.15,
        )
        txt.set_picker(True)
        click_targets[txt] = ("__behavior__", behavior, icon_path)

    sessions_expanded = True

    sessions_header_y = 0.34
    sessions_top_y = 0.30
    row_h_sess = 0.042

    sessions_header = panel.text(
        0.02,
        sessions_header_y,
        "▼ Players (click to collapse)",
        transform=panel.transAxes,
        va="center",
        fontsize=11,
        weight="bold",
    )
    sessions_header.set_picker(True)
    click_targets[sessions_header] = ("__toggle_sessions__",)

    session_row_artists = []

    for j, player_id in enumerate(unique_players):
        yy = sessions_top_y - j * row_h_sess
        if yy < 0.16:
            break

        dot = panel.scatter(
            0.04,
            yy,
            transform=panel.transAxes,
            s=70,
            marker="o",
            color=session_color_map[player_id],
            edgecolors="black",
            linewidths=0.6,
            zorder=3,
        )
        txt = panel.text(
            0.10,
            yy,
            str(player_id),
            transform=panel.transAxes,
            va="center",
            fontsize=9,
        )
        session_row_artists.append((dot, txt))

    def set_sessions_visible(visible: bool):
        for dot, txt in session_row_artists:
            dot.set_visible(visible)
            txt.set_visible(visible)
        fig.canvas.draw_idle()

    def update_sessions_header():
        sessions_header.set_text(
            "▼ Players (click to collapse)" if sessions_expanded else "► Players (click to expand)"
        )
        fig.canvas.draw_idle()

    if explained_variance is not None and hasattr(projection_model, "components_"):
        pc1_text = _build_pc_text(projection_model, feature_names, 0)
        pc2_text = _build_pc_text(projection_model, feature_names, 1)

        interpretation_text = (
            "PCA Dimension Interpretation\n\n"
            f"PC1 ({explained_variance[0] * 100:.1f}% variance)\n"
            f"{pc1_text}\n\n"
            f"PC2 ({explained_variance[1] * 100:.1f}% variance)\n"
            f"{pc2_text}\n\n"
            "Engineered features\n"
            + "\n".join(f"- {f}" for f in feature_names)
        )
    else:
        interpretation_text = "Engineered features\n" + "\n".join(f"- {f}" for f in feature_names)

    panel.text(
        0.02,
        0.13,
        interpretation_text,
        transform=panel.transAxes,
        va="top",
        fontsize=9,
        bbox=dict(boxstyle="round,pad=0.5", facecolor="white", edgecolor="gray"),
    )

    def on_pick(event):
        nonlocal sessions_expanded

        artist = event.artist

        if artist in click_targets:
            payload = click_targets[artist]
            kind = payload[0]

            if kind == "__toggle_sessions__":
                sessions_expanded = not sessions_expanded
                set_sessions_visible(sessions_expanded)
                update_sessions_header()
                return

            if kind == "__behavior__":
                _, behavior, icon_path = payload
                if icon_path is None:
                    return
                open_expanded(icon_path, title=f"Behavior {behavior}")
                return

        if artist in point_targets:
            idx = point_targets[artist]
            entry = clustered_entries[idx]

            info = entry["info"]
            behavior = canonical_behavior_tuple_from_info(info)
            icon_path = behavior_to_icon_path(behavior, ICON_DIR)
            video_path = find_video_for_entry(entry, VIDEO_ROOT_DIR)

            open_session_popup(entry, icon_path, video_path)

            if video_path:
                print(f"Opening replay: {video_path}")
                open_video_file(video_path)
            else:
                print(
                    "No matching replay found for "
                    f"playerId={info.get('playerId')}, "
                    f"behavior={behavior_hash8(behavior)}, "
                    f"levelPlayID={info.get('levelPlayID')}"
                )

    fig.canvas.mpl_connect("pick_event", on_pick)

    set_sessions_visible(sessions_expanded)
    update_sessions_header()

    video_root = Path(VIDEO_ROOT_DIR)
    if not video_root.exists():
        print(f"\nWARNING: VIDEO_ROOT_DIR does not exist: {VIDEO_ROOT_DIR}")
    else:
        count_videos = sum(1 for _ in iter_video_files(VIDEO_ROOT_DIR))
        print(f"\nCSV: {CSV_PATH}")
        print(f"Included players: {INCLUDE_PLAYER_IDS if INCLUDE_PLAYER_IDS else 'ALL'}")
        print(f"Video search root: {video_root.resolve()}")
        print(f"Found {count_videos} candidate video files recursively.")

    if missing_icons:
        print("\nWARNING: Missing behavior icons for these behavior tuples:")
        for behavior in missing_icons:
            expected = os.path.join(ICON_DIR, f"behavior_{behavior_hash8(behavior)}.png")
            print(f"  {behavior} -> expected something like: {expected}")

    plt.show()


if __name__ == "__main__":
    visualize_player_clusters()