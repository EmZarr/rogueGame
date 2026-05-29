import os
import hashlib
import subprocess
import sys
from pathlib import Path

import numpy as np
import matplotlib
import matplotlib.pyplot as plt
import matplotlib.image as mpimg
import colorsys

from ClusterProject import cluster_entries


ICON_DIR = "icons"
VIDEO_ROOT_DIR = "MyRecordings"
PANEL_WIDTH = 0.33

VIDEO_EXTS = {".mp4", ".mov", ".mkv", ".avi", ".webm", ".m4v"}

# Marker shapes recycled across behaviors. Matplotlib only has ~10 distinct ones.
MARKERS = ["o", "s", "^", "D", "P", "X", "*", "v", "<", ">"]

# (label, index in canonical 6-tuple, max value)
# Maxes are the actual bin counts coming out of the C# fitness/behavior code:
#   geo openness          → 12 bins  (GeoFitAndBehav uses GetBehaviorRangeSmooth(12, ...))
#   loot density          → 3 bins
#   obstacle density      → 3 bins
#   enemy composition     → 126 bins (resolution=20 in EnemFitAndBehav)
#   enemy difficulty      → 3 bins
# Adjust the labels here to whatever names you use in your writeup.
BEHAVIOR_FIELDS = [
    ("Openess", 0, 12),
    ("Loot on Main Ratio", 2, 3),
    ("Loot Health Ratio", 3, 3),
    ("Enemy Encounter Type", 4, 126),
    ("Difficulty", 5, 3),
]

# Short forms used in the side-panel behavior rows so each behavior
# fits on one line. Edit if you change the labels above.
COMPACT_LABELS = {
    "Openess": "Openess",
    "Loot on Main Ratio": "LootMPR",
    "Loot Health Ratio": "LootHR",
    "Enemy Encounter Type": "Enemy",
    "Difficulty": "Diff",
}

# Alpha levels for the three filter states
DEFAULT_ALPHA = 0.75      # no filter active
HIGHLIGHT_ALPHA = 0.95    # point matches the active filter
DIM_ALPHA = 0.08          # point does not match the active filter


# Clearly distinguishable baseline colors
BASE_PLAYER_COLORS = [
    "#e41a1c",  # red
    "#377eb8",  # blue
    "#4daf4a",  # green
    "#984ea3",  # purple
    "#ff7f00",  # orange
    "#ffff33",  # yellow
    "#a65628",  # brown
    "#f781bf",  # pink
    "#17becf",  # cyan
    "#999999",  # gray
]

def _hex_to_rgb01(hex_color: str):
    hex_color = hex_color.lstrip("#")
    return tuple(int(hex_color[i:i+2], 16) / 255.0 for i in (0, 2, 4))


def session_to_color(session_id: str, player_index: int):
    """
    Assign highly distinct colors first.
    After the base palette is exhausted, generate deterministic
    variants using the player hash.
    """

    # First N players get maximally distinct colors
    if player_index < len(BASE_PLAYER_COLORS):
        return _hex_to_rgb01(BASE_PLAYER_COLORS[player_index])

    # Additional players:
    # deterministic hue generation from hash
    hash_val = int(
        hashlib.md5(str(session_id).encode("utf-8")).hexdigest(),
        16
    )

    rng = np.random.RandomState(hash_val % (2**32))

    # Generate vivid colors with constrained saturation/value
    hue = rng.rand()
    sat = 0.65 + rng.rand() * 0.25
    val = 0.75 + rng.rand() * 0.20

    return colorsys.hsv_to_rgb(hue, sat, val)


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
    """Multi-line verbose label (used in the hover tooltip)."""
    lines = []
    for name, idx, mx in BEHAVIOR_FIELDS:
        val = int(behavior_tuple[idx])
        lines.append(f"{name}: {val}/{mx}")
    return "\n".join(lines)


def format_behavior_compact(behavior_tuple: tuple) -> str:
    """Single-line compact label (used in the side-panel rows)."""
    parts = []
    for name, idx, mx in BEHAVIOR_FIELDS:
        val = int(behavior_tuple[idx])
        short = COMPACT_LABELS.get(name, name)
        parts.append(f"{short} {val}/{mx}")
    return "  ·  ".join(parts)


def open_expanded(image_path: str, title: str):
    img = mpimg.imread(image_path)
    fig = plt.figure(figsize=(4.5, 4.5))
    ax = fig.add_subplot(111)
    ax.imshow(img)
    ax.set_title(title, fontsize=10)
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
        level_tokens.append(level_play_id)

    return {
        "player_id": player_id,
        "behavior_hash": beh_hash,
        "level_tokens": level_tokens,
    }


def iter_video_files(root_dir: str):
    root = Path(root_dir)
    if not root.exists():
        return
    for path in root.rglob("*"):
        if path.is_file() and path.suffix.lower() in VIDEO_EXTS:
            yield path


def find_video_for_entry(entry: dict, root_dir: str) -> str | None:
    tokens = build_video_match_tokens(entry)
    player_id = tokens["player_id"]
    beh_hash = tokens["behavior_hash"]
    level_tokens = tokens["level_tokens"]

    best_match = None
    best_score = -1

    for video_path in iter_video_files(root_dir):
        name = video_path.name.lower()

        score = 0

        if player_id and player_id.lower() in name:
            score += 3

        if beh_hash and beh_hash.lower() in name:
            score += 2

        for lt in level_tokens:
            if lt and lt.lower() in name:
                score += 2

        if score > best_score:
            best_score = score
            best_match = str(video_path)

    if best_score <= 0:
        return None

    return best_match


def open_video_file(video_path: str):
    try:
        if sys.platform.startswith("win"):
            os.startfile(video_path)  # type: ignore[attr-defined]
        elif sys.platform == "darwin":
            subprocess.Popen(["open", video_path])
        else:
            subprocess.Popen(["xdg-open", video_path])
    except Exception as e:
        print(f"Could not open video file: {e}")


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
    session_color_map = {player_id: session_to_color(player_id, i) for i, player_id in enumerate(unique_players)}
    behavior_marker_map = {b: MARKERS[i % len(MARKERS)] for i, b in enumerate(unique_behaviors)}

    # Kept for informational printing only — clusters are conveyed via spatial grouping + tooltip.
    unique_clusters = sorted({
        int(entry["info"]["cluster_label"])
        for entry in clustered_entries
        if entry["info"].get("cluster_label") is not None
    })

    explained_variance = getattr(projection_model, "explained_variance_ratio_", None)

    fig = plt.figure(figsize=(16, 10))

    ax = fig.add_axes([0.07, 0.10, 0.88 - PANEL_WIDTH, 0.83])
    panel = fig.add_axes([0.07 + (0.88 - PANEL_WIDTH) + 0.02, 0.10, PANEL_WIDTH - 0.04, 0.83])
    panel.set_axis_off()

    point_targets = {}     # scatter → entry index (used by pick handler for popup)
    click_targets = {}     # text/scatter artist → payload (used by pick handler for panel)
    point_metadata = []    # ordered list of {scatter, player_id, behavior, cluster_label}
    missing_icons = []

    # ---------------- main scatter plot ----------------
    # Color = player, shape = behavior. Cluster info is revealed in the tooltip.
    for i, entry in enumerate(clustered_entries):
        info = entry["info"]
        player_id = info["playerId"]
        behavior = canonical_behavior_tuple_from_info(info)
        cluster_label = info.get("cluster_label")

        scatter = ax.scatter(
            coords[i, 0],
            coords[i, 1],
            c=[session_color_map[player_id]],
            marker=behavior_marker_map[behavior],
            s=80,
            alpha=DEFAULT_ALPHA,
            edgecolors="black",
            linewidth=0.4,
            picker=True,
        )
        point_targets[scatter] = i
        point_metadata.append({
            "scatter": scatter,
            "player_id": player_id,
            "behavior": behavior,
            "cluster_label": cluster_label,
            "index": i,
        })

    # ---------------- cluster centers (black X markers) ----------------
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
        "Player Clustering - Experiment 3-B Combat(PCA Projection)\n"
        "Color = Player | Shape = Behavior | Hover for details | Click a row to filter | Click a point to open replay"
    )
    ax.grid(True, alpha=0.25)

    # ---------------- filter state ----------------
    # Use single-item lists so the closures below can mutate them.
    clicked_filter = [None]   # persistent filter set by clicking a row
    hover_filter = [None]     # temporary filter set by hovering a point

    def get_effective_filter():
        return hover_filter[0] if hover_filter[0] is not None else clicked_filter[0]

    def apply_filter():
        eff = get_effective_filter()
        for meta in point_metadata:
            if eff is None:
                meta["scatter"].set_alpha(DEFAULT_ALPHA)
            else:
                kind, value = eff
                if kind == "player":
                    match = meta["player_id"] == value
                elif kind == "behavior":
                    match = meta["behavior"] == value
                else:
                    match = True
                meta["scatter"].set_alpha(HIGHLIGHT_ALPHA if match else DIM_ALPHA)
        fig.canvas.draw_idle()

    # ---------------- right side panel ----------------

    # "Show all" reset row at the very top
    reset_artist = panel.text(
        0.02,
        0.985,
        "↺ Show all (clear filter)",
        transform=panel.transAxes,
        va="top",
        fontsize=10,
        weight="bold",
        color="#444444",
    )
    reset_artist.set_picker(True)
    click_targets[reset_artist] = ("__reset_filter__",)

    panel.text(
        0.02,
        0.94,
        "Level Descriptors (click to filter / open map)",
        transform=panel.transAxes,
        va="top",
        fontsize=11,
        weight="bold",
    )

    col1_x_marker, col1_x_text = 0.03, 0.07

    row_h_beh = 0.045
    top_beh = 0.89

    # Number of visible rows in the viewport
    max_visible_behavior_rows = 8

    behavior_scroll_offset = [0]
    behavior_row_artists = []

    # Scroll area bounds
    behavior_cutoff_y = top_beh - max_visible_behavior_rows * row_h_beh

    # Visual background box
    from matplotlib.patches import Rectangle

    behavior_scroll_box = Rectangle(
        (0.005, behavior_cutoff_y - 0.005),
        0.99,
        top_beh - behavior_cutoff_y + row_h_beh * 0.5 + 0.005,
        transform=panel.transAxes,
        fill=True,
        facecolor="#fafafa",
        edgecolor="#cccccc",
        linewidth=0.8,
        zorder=0,
    )

    panel.add_patch(behavior_scroll_box)

    # Create ALL behavior rows once
    for i, behavior in enumerate(unique_behaviors):

        initial_y = top_beh - i * row_h_beh

        marker_artist = panel.scatter(
            col1_x_marker,
            initial_y,
            transform=panel.transAxes,
            s=60,
            marker=behavior_marker_map[behavior],
            color="lightgray",
            edgecolors="black",
            linewidths=0.6,
            zorder=3,
            picker=True,
        )

        icon_path = behavior_to_icon_path(behavior, ICON_DIR)

        label = format_behavior_compact(behavior)

        if icon_path is None:
            missing_icons.append(behavior)

        txt = panel.text(
            col1_x_text,
            initial_y,
            label,
            transform=panel.transAxes,
            va="center",
            fontsize=8,
            picker=True,
        )

        click_targets[txt] = ("__behavior__", behavior, icon_path)
        click_targets[marker_artist] = ("__behavior__", behavior, icon_path)

        behavior_row_artists.append((marker_artist, txt))

    behavior_overflow_artist = panel.text(
        0.02,
        behavior_cutoff_y - 0.005,
        "",
        transform=panel.transAxes,
        va="top",
        fontsize=7,
        style="italic",
        color="#888888",
    )

    def update_behavior_rows():

        n = len(behavior_row_artists)

        max_offset = max(0, n - max_visible_behavior_rows)

        behavior_scroll_offset[0] = max(
            0,
            min(behavior_scroll_offset[0], max_offset)
        )

        for i, (marker_artist, txt) in enumerate(behavior_row_artists):

            visual_index = i - behavior_scroll_offset[0]

            if 0 <= visual_index < max_visible_behavior_rows:

                target_y = top_beh - visual_index * row_h_beh

                marker_artist.set_offsets([[col1_x_marker, target_y]])
                txt.set_position((col1_x_text, target_y))

                marker_artist.set_visible(True)
                txt.set_visible(True)

            else:
                marker_artist.set_visible(False)
                txt.set_visible(False)

        above = behavior_scroll_offset[0]
        below = max(0, n - max_visible_behavior_rows - behavior_scroll_offset[0])

        parts = []

        if above > 0:
            parts.append(f"↑ {above} above")

        if below > 0:
            parts.append(f"↓ {below} below")

        if parts:
            behavior_overflow_artist.set_text(
                "  │  ".join(parts) + "   (scroll over levels)"
            )
            behavior_overflow_artist.set_visible(True)
        else:
            behavior_overflow_artist.set_visible(False)

        fig.canvas.draw_idle()

    update_behavior_rows()

    sessions_expanded = True

    sessions_header_y = behavior_cutoff_y - 0.06
    sessions_top_y = sessions_header_y - 0.035
    row_h_sess = 0.026
    visible_session_rows = 7
    sessions_cutoff = sessions_top_y - visible_session_rows * row_h_sess

    sessions_header = panel.text(
        0.02,
        sessions_header_y,
        "▼ Players (scroll / click to filter)",
        transform=panel.transAxes,
        va="center",
        fontsize=10,
        weight="bold",
    )
    sessions_header.set_picker(True)
    click_targets[sessions_header] = ("__toggle_sessions__",)

    # Bordered rectangle around the scrollable area — makes it visually obvious
    # that this is a scroll region.
    from matplotlib.patches import Rectangle
    scroll_box = Rectangle(
        (0.005, sessions_cutoff - 0.005),
        0.99,
        sessions_top_y - sessions_cutoff + row_h_sess * 0.5 + 0.005,
        transform=panel.transAxes,
        fill=True,
        facecolor="#fafafa",
        edgecolor="#cccccc",
        linewidth=0.8,
        zorder=0,
    )
    panel.add_patch(scroll_box)

    # How many player rows fit in the viewport at once
    max_visible_rows = visible_session_rows

    session_row_artists = []

    # Create ALL player rows. Visibility + position is then managed by
    # update_player_rows() based on scroll_offset, so the visible rows
    # form a sliding window over this list.
    for j, player_id in enumerate(unique_players):
        initial_y = sessions_top_y - j * row_h_sess  # will be updated immediately

        dot = panel.scatter(
            0.03,
            initial_y,
            transform=panel.transAxes,
            s=45,
            marker="o",
            color=session_color_map[player_id],
            edgecolors="black",
            linewidths=0.5,
            zorder=3,
            picker=True,
        )

        # Truncate long hash-style IDs so they actually fit in the panel
        pid_str = str(player_id)
        short_pid = pid_str[:10] + "…" if len(pid_str) > 11 else pid_str

        txt = panel.text(
            0.08,
            initial_y,
            short_pid,
            transform=panel.transAxes,
            va="center",
            fontsize=8,
            picker=True,
        )

        click_targets[dot] = ("__player__", player_id)
        click_targets[txt] = ("__player__", player_id)
        session_row_artists.append((dot, txt))

    # Status line that shows what's hidden above/below the viewport
    overflow_artist = panel.text(
        0.02,
        sessions_cutoff - 0.005,
        "",
        transform=panel.transAxes,
        va="top",
        fontsize=7,
        style="italic",
        color="#888888",
    )

    scroll_offset = [0]

    def update_player_rows():
        n = len(session_row_artists)
        max_offset = max(0, n - max_visible_rows)
        scroll_offset[0] = max(0, min(scroll_offset[0], max_offset))

        for j, (dot, txt) in enumerate(session_row_artists):
            visual_index = j - scroll_offset[0]
            if 0 <= visual_index < max_visible_rows:
                target_y = sessions_top_y - visual_index * row_h_sess
                dot.set_offsets([[0.03, target_y]])
                txt.set_position((0.08, target_y))
                dot.set_visible(sessions_expanded)
                txt.set_visible(sessions_expanded)
            else:
                dot.set_visible(False)
                txt.set_visible(False)

        above = scroll_offset[0]
        below = max(0, n - max_visible_rows - scroll_offset[0])
        parts = []
        if above > 0:
            parts.append(f"↑ {above} above")
        if below > 0:
            parts.append(f"↓ {below} below")
        if parts and sessions_expanded:
            overflow_artist.set_text("  │  ".join(parts) + "   (scroll over players to see more)")
            overflow_artist.set_visible(True)
        else:
            overflow_artist.set_visible(False)

        fig.canvas.draw_idle()

    def set_sessions_visible(visible: bool):
        if not visible:
            # Collapsed: hide every player artist plus the overflow line
            for dot, txt in session_row_artists:
                dot.set_visible(False)
                txt.set_visible(False)
            overflow_artist.set_visible(False)
            fig.canvas.draw_idle()
        else:
            # Expanded: let update_player_rows pick which rows are visible
            update_player_rows()

    def update_sessions_header():
        sessions_header.set_text(
            "▼ Players (scroll / click to filter)" if sessions_expanded else "► Players (click to expand)"
        )
        fig.canvas.draw_idle()

    def on_scroll(event):

        contains, _ = panel.contains(event)

        if not contains:
            return

        delta = 1 if event.button == "down" else -1

        renderer = fig.canvas.get_renderer()

        #
        # Detect whether mouse is inside the BEHAVIOR scroll box
        #
        behavior_bbox = behavior_scroll_box.get_window_extent(renderer)

        if behavior_bbox.contains(event.x, event.y):

            new_offset = behavior_scroll_offset[0] + delta

            n = len(behavior_row_artists)

            max_offset = max(0, n - max_visible_behavior_rows)

            new_offset = max(0, min(new_offset, max_offset))

            if new_offset != behavior_scroll_offset[0]:
                behavior_scroll_offset[0] = new_offset
                update_behavior_rows()

            return

        #
        # Detect whether mouse is inside the PLAYER scroll box
        #
        scroll_bbox = scroll_box.get_window_extent(renderer)

        if scroll_bbox.contains(event.x, event.y):

            if not sessions_expanded:
                return

            new_offset = scroll_offset[0] + delta

            n = len(session_row_artists)

            max_offset = max(0, n - max_visible_rows)

            new_offset = max(0, min(new_offset, max_offset))

            if new_offset != scroll_offset[0]:
                scroll_offset[0] = new_offset
                update_player_rows()

    fig.canvas.mpl_connect("scroll_event", on_scroll)


    if explained_variance is not None and hasattr(projection_model, "components_"):
        # Show every PC the model produced, not just PC1/PC2. One line each
        # with the top-2 loadings inline so the box stays compact even for
        # 5+ components.
        lines = ["PCA Dimensions (top loadings)", ""]
        for i, var in enumerate(explained_variance):
            pc_text = _build_pc_text(projection_model, feature_names, i, top_n=2)
            inline = ", ".join(pc_text.splitlines())
            lines.append(f"PC{i+1} ({var * 100:.1f}%): {inline}")
        interpretation_text = "\n".join(lines)
    else:
        interpretation_text = "(no PCA loadings available)"

    # Anchor PCA box dynamically right below the scroll box so it gets all leftover vertical space.
    pca_anchor_y = sessions_cutoff - 0.05

    panel.text(
        0.02,
        pca_anchor_y,
        interpretation_text,
        transform=panel.transAxes,
        va="top",
        fontsize=9,
        bbox=dict(boxstyle="round,pad=0.5", facecolor="white", edgecolor="gray"),
    )

    # ---------------- click handler ----------------
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

            if kind == "__reset_filter__":
                clicked_filter[0] = None
                apply_filter()
                return

            if kind == "__behavior__":
                _, behavior, icon_path = payload

                # Toggle behavior filter on/off
                new_filter = ("behavior", behavior)
                clicked_filter[0] = None if clicked_filter[0] == new_filter else new_filter
                apply_filter()

                # Also open the map preview if we have one
                if icon_path:
                    open_expanded(icon_path, title=f"Behavior {behavior}")
                return

            if kind == "__player__":
                _, player_id = payload

                # Toggle player filter on/off
                new_filter = ("player", player_id)
                clicked_filter[0] = None if clicked_filter[0] == new_filter else new_filter
                apply_filter()
                return

        # Click on a plot point → open the session popup (and replay if it exists)
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

    # ---------------- hover tooltip + per-point highlight ----------------
    # One reusable annotation that follows the mouse and shows what's under it.
    hover_annotation = ax.annotate(
        "",
        xy=(0, 0),
        xytext=(15, 15),
        textcoords="offset points",
        bbox=dict(boxstyle="round,pad=0.5", facecolor="#fffaf0", edgecolor="black", linewidth=0.8),
        fontsize=9,
        zorder=10,
        visible=False,
    )

    # Default visual style for an unselected point (so we can restore it after un-hovering)
    DEFAULT_EDGE_COLOR = "black"
    DEFAULT_EDGE_WIDTH = 0.4
    DEFAULT_SIZE = 80
    HIGHLIGHT_EDGE_COLOR = "#ffcc00"   # bright yellow outline
    HIGHLIGHT_EDGE_WIDTH = 2.5
    HIGHLIGHT_SIZE = 200

    # Track which scatter currently has the "you are hovering me" ring
    currently_highlighted = [None]

    def set_highlighted_scatter(scatter):
        prev = currently_highlighted[0]
        if prev is not None and prev is not scatter:
            prev.set_edgecolors(DEFAULT_EDGE_COLOR)
            prev.set_linewidths(DEFAULT_EDGE_WIDTH)
            prev.set_sizes([DEFAULT_SIZE])
            prev.set_zorder(2)
        if scatter is not None:
            scatter.set_edgecolors(HIGHLIGHT_EDGE_COLOR)
            scatter.set_linewidths(HIGHLIGHT_EDGE_WIDTH)
            scatter.set_sizes([HIGHLIGHT_SIZE])
            scatter.set_zorder(5)
        currently_highlighted[0] = scatter

    def build_tooltip_text(meta):
        # Compact, multi-line tooltip
        behavior_lines = format_behavior_rows(meta["behavior"]).split("\n")
        return (
            f"Player: {meta['player_id']}\n"
            f"Cluster: {meta['cluster_label']}\n"
            + "\n".join(behavior_lines)
        )

    def clear_hover():
        changed = False
        if hover_filter[0] is not None:
            hover_filter[0] = None
            changed = True
        if currently_highlighted[0] is not None:
            set_highlighted_scatter(None)
            changed = True
        if hover_annotation.get_visible():
            hover_annotation.set_visible(False)
            changed = True
        if changed:
            apply_filter()

    def on_motion(event):
        # Cursor outside the plot → reset everything hover-related
        if event.inaxes != ax:
            clear_hover()
            return

        # Find the topmost point under the cursor (newest scatter wins on overlap)
        for meta in reversed(point_metadata):
            contains, _ = meta["scatter"].contains(event)
            if contains:
                new_hover = ("player", meta["player_id"])

                # Update tooltip text + position
                hover_annotation.xy = (event.xdata, event.ydata)
                hover_annotation.set_text(build_tooltip_text(meta))
                hover_annotation.set_visible(True)

                # Highlight the specific point under the cursor
                set_highlighted_scatter(meta["scatter"])

                if hover_filter[0] != new_hover:
                    hover_filter[0] = new_hover
                    apply_filter()
                else:
                    # Filter unchanged but tooltip moved → still need a redraw
                    fig.canvas.draw_idle()
                return

        # Cursor is inside the plot but not over any point
        clear_hover()

    fig.canvas.mpl_connect("motion_notify_event", on_motion)

    set_sessions_visible(sessions_expanded)
    update_sessions_header()

    video_root = Path(VIDEO_ROOT_DIR)
    if not video_root.exists():
        print(f"\nWARNING: VIDEO_ROOT_DIR does not exist: {VIDEO_ROOT_DIR}")
    else:
        count_videos = sum(1 for _ in iter_video_files(VIDEO_ROOT_DIR))
        print(f"\nIncluded clusters: {unique_clusters}")
        print(f"Included players ({len(unique_players)}):")
        for p in unique_players:
            print(f"  - {p}")
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