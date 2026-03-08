# TelemetryVisualizer.py
# ------------------------------------------------------------
# 3D PCA telemetry plot with a custom right-side panel:
# - KMeans clustering + PCA (3D)
# - Color per sessionId (deterministic)
# - Marker shape per behavior combination (deterministic by first-seen order)
# - Right panel has THREE sections:
#     1) Behaviors (2 columns): marker + named rows; click text to expand icon
#     2) Sessions (collapsible): click header to expand/collapse sessionId -> color list
#     3) PCA interpretation text box
#
# Click behavior text in right panel:
#   -> opens the map icon
#
# Click a point in the 3D scatter:
#   -> finds a replay video by matching:
#        - sessionId
#        - behavior hash
#        - date+minute from timestamp
#   -> opens a popup with info/icon
#   -> opens the video in the OS default player if found
#
# Video lookup:
# - Searches recursively under VIDEO_ROOT_DIR
# - Expects video filename to contain:
#       session id
#       behavior hash (same as icon)
#       date+minute
#
# Example good filename patterns:
#   session_ABC_behavior_faa7ad14_2026-03-07_14-23.mp4
#   ABC_behavior_faa7ad14_time_14-23_date_2026-03-07.mp4
#
# Icons:
# - Put behavior icons in ICON_DIR:
#     icons/behavior_<hash8>.png (or .jpg/.jpeg/.webp)
# - hash8 = md5(repr(behavior_tuple))[:8]
# ------------------------------------------------------------

import os
import re
import sys
import hashlib
import subprocess
from pathlib import Path

import numpy as np
import pandas as pd
import matplotlib.pyplot as plt
import matplotlib.image as mpimg

from sklearn.cluster import KMeans
from sklearn.decomposition import PCA
from sklearn.preprocessing import StandardScaler
from scipy.spatial import ConvexHull


# ==========================
# CONFIG
# ==========================
CSV_PATH = "Telemetry_Raw.csv"
N_CLUSTERS = 5

ICON_DIR = "icons"
VIDEO_ROOT_DIR = "MyRecordings"

PANEL_WIDTH = 0.33  # fraction of figure width reserved for the right panel

# Columns used to build the behavior tuple
behavior_columns = [
    "GeometryBehavior",
    "FurnishingBehaviorSpread",
    "FurnishingBehaviorRatio",
    "EnemyBehaviorRatio",
    "EnemyBehaviorDifficulty",
]

# Display mapping for behavior tuple fields.
# behavior tuple is: (geo_x, geo_y, furn_x, furn_y, enem_x, enem_y)
# geo_y is forced to 0 to match your icon generator.
BEHAVIOR_FIELDS = [
    ("Room Amount Tier", 0, 10),
    ("Loot on Main Ratio", 2, 5),
    ("Loot Health Ratio", 3, 5),
    ("Enemy Encounter Type", 4, 126),
    ("Difficulty", 5, 5),
]

MARKERS = ["o", "s", "^", "D", "P", "X", "*", "v", "<", ">"]
VIDEO_EXTS = {".mp4", ".mov", ".mkv", ".avi", ".webm", ".m4v"}


# ==========================
# HELPERS
# ==========================
def session_to_color(session_id: str):
    """Deterministically map session_id -> RGB tuple."""
    hash_val = int(hashlib.md5(str(session_id).encode("utf-8")).hexdigest(), 16)
    rng = np.random.RandomState(hash_val % (2**32))
    return tuple(rng.rand(3))


def behavior_hash8(behavior_tuple) -> str:
    """Stable short hash for a behavior tuple."""
    s = repr(tuple(behavior_tuple))
    return hashlib.md5(s.encode("utf-8")).hexdigest()[:8]


def behavior_to_icon_path(behavior_tuple) -> str | None:
    """Default icon scheme: icons/behavior_<hash8>.(png|jpg|jpeg|webp)"""
    h = behavior_hash8(behavior_tuple)
    for ext in (".png", ".jpg", ".jpeg", ".webp"):
        p = os.path.join(ICON_DIR, f"behavior_{h}{ext}")
        if os.path.exists(p):
            return p
    return None


def format_behavior_rows(b: tuple) -> str:
    """Convert behavior tuple -> multi-line label with per-field max."""
    lines = []
    for name, idx, mx in BEHAVIOR_FIELDS:
        val = int(b[idx])
        lines.append(f"{name}: {val}/{mx}")
    return "\n".join(lines)


def open_expanded(image_path: str, title: str):
    """Open a popup window showing the icon larger."""
    img = mpimg.imread(image_path)
    f2 = plt.figure(figsize=(7, 7))
    ax2 = f2.add_subplot(111)
    ax2.imshow(img)
    ax2.set_title(title)
    ax2.axis("off")
    f2.tight_layout()
    f2.show()


def csv_behavior_tuple(row) -> tuple:
    """
    Build the canonical behavior tuple used for marker mapping + icon hashing.
    """
    geo_x = int(row["GeometryBehavior"])
    geo_y = 0  # forced to match your icon generator
    furn_x = int(row["FurnishingBehaviorSpread"])
    furn_y = int(row["FurnishingBehaviorRatio"])
    enem_x = int(row["EnemyBehaviorRatio"])
    enem_y = int(row["EnemyBehaviorDifficulty"])
    return (geo_x, geo_y, furn_x, furn_y, enem_x, enem_y)


def normalize_timestamp_for_video_match(value) -> tuple[str | None, str | None]:
    """
    Convert timestamp/date into:
      - date string: YYYY-MM-DD
      - minute string: HH-MM

    Examples:
      2026-03-07 14:23:51 -> ("2026-03-07", "14-23")
      2026/03/07 14:23    -> ("2026-03-07", "14-23")

    Returns (None, None) if parsing fails.
    """
    if pd.isna(value):
        return None, None

    s = str(value).strip()
    if not s:
        return None, None

    dt = pd.to_datetime(s, errors="coerce")
    if pd.notna(dt):
        return dt.strftime("%Y-%m-%d"), dt.strftime("%H-%M")

    # fallback regex
    m = re.search(
        r"(\d{4})[-_/](\d{1,2})[-_/](\d{1,2})[ T](\d{1,2})[:\-](\d{1,2})",
        s,
    )
    if m:
        yyyy, mm, dd, hh, mi = m.groups()
        return f"{int(yyyy):04d}-{int(mm):02d}-{int(dd):02d}", f"{int(hh):02d}-{int(mi):02d}"

    return None, None


def build_video_match_tokens(row) -> dict:
    """
    Build a set of acceptable filename tokens from the CSV row.
    """
    session_id = str(row["sessionId"])
    behavior = csv_behavior_tuple(row)
    beh_hash = behavior_hash8(behavior)

    match_date = row.get("match_date", None)
    match_minute = row.get("match_minute", None)

    date_tokens = []
    minute_tokens = []
    datetime_tokens = []

    if match_date:
        date_tokens = [
            match_date,                        # 2026-03-07
            match_date.replace("-", "_"),      # 2026_03_07
            match_date.replace("-", ""),       # 20260307
        ]

    if match_minute:
        minute_tokens = [
            match_minute,                      # 14-23
            match_minute.replace("-", "_"),    # 14_23
            match_minute.replace("-", ":"),    # 14:23
            match_minute.replace("-", ""),     # 1423
        ]

    if match_date and match_minute:
        yyyyMMdd = match_date.replace("-", "")
        hhmm = match_minute.replace("-", "")
        datetime_tokens = [
            f"{match_date}_{match_minute}",              # 2026-03-07_14-23
            f"{match_date}_{match_minute.replace('-', '_')}",
            f"{match_date} {match_minute.replace('-', ':')}",
            f"{yyyyMMdd}_{hhmm}",                        # 20260307_1423
            f"{yyyyMMdd}-{hhmm}",                        # 20260307-1423
            f"{yyyyMMdd}{hhmm}",                         # 202603071423
        ]

    return {
        "session_id": session_id,
        "behavior_hash": beh_hash,
        "date_tokens": date_tokens,
        "minute_tokens": minute_tokens,
        "datetime_tokens": datetime_tokens,
    }


def iter_video_files(root_dir: str):
    """
    Recursively iterate all candidate video files under root_dir.
    """
    root = Path(root_dir)
    if not root.exists():
        return

    for p in root.rglob("*"):
        if p.is_file() and p.suffix.lower() in VIDEO_EXTS:
            yield p


def find_video_for_row(row) -> str | None:
    """
    Find a replay video whose filename contains:
      - sessionId
      - behavior hash
      - date
      - minute

    Searches recursively under VIDEO_ROOT_DIR.

    If multiple candidates match, prefers the one whose name matches more tokens,
    then newest modification time.
    """
    tokens = build_video_match_tokens(row)

    session_id = tokens["session_id"]
    behavior_hash = tokens["behavior_hash"]
    date_tokens = tokens["date_tokens"]
    minute_tokens = tokens["minute_tokens"]
    datetime_tokens = tokens["datetime_tokens"]

    candidates = []

    for p in iter_video_files(VIDEO_ROOT_DIR) or []:
        name = p.name

        if session_id not in name:
            continue
        if behavior_hash not in name:
            continue

        score = 0

        if any(tok in name for tok in datetime_tokens):
            score += 4

        if any(tok in name for tok in date_tokens):
            score += 2

        if any(tok in name for tok in minute_tokens):
            score += 2

        # Require at least date+minute coverage if timestamp info exists
        if (date_tokens or minute_tokens) and score < 4:
            continue

        candidates.append((score, p.stat().st_mtime, p))

    if not candidates:
        return None

    candidates.sort(key=lambda x: (x[0], x[1]), reverse=True)
    return str(candidates[0][2])


def open_video_file(video_path: str):
    """
    Open video in OS default player.
    """
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


def open_session_popup(row, icon_path: str | None, video_path: str | None):
    """
    Show a popup with row metadata, behavior hash, icon, and resolved video path.
    """
    behavior = csv_behavior_tuple(row)
    beh_hash = behavior_hash8(behavior)

    fig2 = plt.figure(figsize=(9, 7))
    ax2 = fig2.add_subplot(111)
    ax2.axis("off")

    if icon_path and os.path.exists(icon_path):
        try:
            img = mpimg.imread(icon_path)
            ax2.imshow(img, extent=[0.04, 0.48, 0.35, 0.95], aspect="auto")
        except Exception as e:
            print(f"Could not load icon: {e}")

    text_lines = [
        f"Session: {row['sessionId']}",
        f"Behavior tuple: {behavior}",
        f"Behavior hash: {beh_hash}",
        f"Date: {row.get('match_date', 'unknown')}",
        f"Minute: {row.get('match_minute', 'unknown')}",
        "",
        f"Video found: {'YES' if video_path else 'NO'}",
        f"Video path: {video_path if video_path else '(no matching file found)'}",
    ]

    ax2.text(
        0.54,
        0.93,
        "\n".join(text_lines),
        transform=ax2.transAxes,
        va="top",
        fontsize=10,
        bbox=dict(boxstyle="round,pad=0.4", facecolor="white", edgecolor="gray"),
    )

    fig2.tight_layout()
    fig2.show()


# ==========================
# LOAD DATA
# ==========================
df = pd.read_csv(CSV_PATH)

if "sessionId" not in df.columns:
    raise ValueError("CSV must contain a 'sessionId' column.")

missing_beh = [c for c in behavior_columns if c not in df.columns]
if missing_beh:
    raise ValueError(f"Missing behavior columns in CSV: {missing_beh}")

# Keep timestamp/date for replay lookup, but do not feed it into PCA.
if "timestamp" in df.columns:
    parsed = df["timestamp"].apply(normalize_timestamp_for_video_match)
    df["match_date"] = parsed.apply(lambda x: x[0])
    df["match_minute"] = parsed.apply(lambda x: x[1])
elif "date" in df.columns:
    parsed = df["date"].apply(normalize_timestamp_for_video_match)
    df["match_date"] = parsed.apply(lambda x: x[0])
    df["match_minute"] = parsed.apply(lambda x: x[1])
else:
    df["match_date"] = None
    df["match_minute"] = None

# Build behavior tuples per row (used for markers + icons)
behavior_combinations = df.apply(csv_behavior_tuple, axis=1)
unique_behaviors = behavior_combinations.unique()

# ==========================
# SELECT FEATURES (NUMERIC ONLY)
# ==========================
excluded = ["sessionId", "timestamp", "date", "match_date", "match_minute"] + behavior_columns
feature_df = df.drop(columns=excluded, errors="ignore").select_dtypes(include=[np.number])

if feature_df.shape[1] == 0:
    raise ValueError(
        "No numeric feature columns found after excluding sessionId + behavior columns. "
        "If you have timestamps/strings, they must be excluded or converted."
    )

X = feature_df.to_numpy()
feature_columns = list(feature_df.columns)
print(f"Using {feature_df.shape[1]} numeric feature columns for clustering.")

# ==========================
# SCALE / KMEANS
# ==========================
scaler = StandardScaler()
X_scaled = scaler.fit_transform(X)

kmeans = KMeans(n_clusters=N_CLUSTERS, random_state=42)
clusters = kmeans.fit_predict(X_scaled)
df["Cluster"] = clusters

# ==========================
# PCA (3D)
# ==========================
pca = PCA(n_components=3, random_state=42)
X_3d = pca.fit_transform(X_scaled)
explained_variance = pca.explained_variance_ratio_

print("\nExplained Variance Ratio:")
for i, var in enumerate(explained_variance):
    print(f"PC{i+1}: {var:.4f} ({var*100:.2f}%)")
print("\nTotal variance captured:", sum(explained_variance) * 100, "%")

# ==========================
# PCA COMPONENT WEIGHTS / INTERPRETATION (prints)
# ==========================
loadings = pd.DataFrame(
    pca.components_.T,
    columns=["PC1", "PC2", "PC3"],
    index=feature_columns,
)

for pc in ["PC1", "PC2", "PC3"]:
    print(f"\nTop contributing features for {pc}:")
    sorted_features = loadings[pc].abs().sort_values(ascending=False)
    for feature in sorted_features.head(10).index:
        weight = loadings.loc[feature, pc]
        print(f"{feature}: {weight:.4f}")

print("\n==============================")
print("CLUSTER BEHAVIOR PROFILES")
print("==============================")

cluster_df = df.copy()
cluster_df["cluster"] = clusters

cluster_means = cluster_df.groupby("cluster")[feature_columns].mean()
global_mean = df[feature_columns].mean()

for c in range(N_CLUSTERS):
    print(f"\nCluster {c}")
    diffs = cluster_means.loc[c] - global_mean
    diffs = diffs.sort_values(key=lambda x: abs(x), ascending=False)

    print("Dominant characteristics:")
    for feature in diffs.head(6).index:
        val = cluster_means.loc[c, feature]
        delta = diffs.loc[feature]
        direction = "higher" if delta > 0 else "lower"
        print(f"  {feature}: {val:.3f} ({direction} than average)")

print("\n==============================")
print("PCA DIMENSION INTERPRETATION")
print("==============================")

for pc in ["PC1", "PC2", "PC3"]:
    print(f"\n{pc} represents:")
    sorted_features = loadings[pc].sort_values(key=lambda x: abs(x), ascending=False)
    for f in sorted_features.head(6).index:
        weight = loadings.loc[f, pc]
        direction = "increases with" if weight > 0 else "decreases with"
        print(f"  {direction} {f} ({weight:.3f})")


def build_pc_interpretation(pc_name: str) -> str:
    sorted_features = loadings[pc_name].sort_values(key=lambda x: abs(x), ascending=False)
    lines = []
    for feature in sorted_features.head(4).index:
        weight = loadings.loc[feature, pc_name]
        sign = "+" if weight > 0 else "-"
        lines.append(f"{sign} {feature}")
    return "\n".join(lines)


pc1_text = build_pc_interpretation("PC1")
pc2_text = build_pc_interpretation("PC2")
pc3_text = build_pc_interpretation("PC3")

# ==========================
# COLOR + SHAPE MAPPING
# ==========================
unique_sessions = df["sessionId"].unique()
session_color_map = {s: session_to_color(s) for s in unique_sessions}

# Marker assignment depends on first-seen order of unique behaviors in the CSV
behavior_marker_map = {b: MARKERS[i % len(MARKERS)] for i, b in enumerate(unique_behaviors)}

# ==========================
# CLUSTER CENTROIDS (PCA SPACE)
# ==========================
centroids_scaled = kmeans.cluster_centers_
centroids_pca = pca.transform(centroids_scaled)

# ==========================
# FIGURE LAYOUT
# ==========================
fig = plt.figure(figsize=(16, 10))

# Left: 3D main plot axis
ax = fig.add_axes([0.07, 0.10, 0.88 - PANEL_WIDTH, 0.83], projection="3d")

# Right: panel axis
panel = fig.add_axes([0.07 + (0.88 - PANEL_WIDTH) + 0.02, 0.10, PANEL_WIDTH - 0.04, 0.83])
panel.set_axis_off()

# ==========================
# CLUSTER HULLS (wireframe)
# ==========================
cluster_colors = plt.cm.tab10(np.linspace(0, 1, N_CLUSTERS))

for i in range(N_CLUSTERS):
    points = X_3d[clusters == i]
    if len(points) < 4:
        continue

    hull = ConvexHull(points)
    for simplex in hull.simplices:
        ax.plot(
            points[simplex, 0],
            points[simplex, 1],
            points[simplex, 2],
            color=cluster_colors[i],
            alpha=0.25,
        )

# ==========================
# MAIN 3D SCATTER (POINT-BY-POINT PICKABLE)
# ==========================
point_targets = {}  # scatter artist -> dataframe row index

for row_idx, row in df.iterrows():
    b = csv_behavior_tuple(row)
    scatter = ax.scatter(
        X_3d[row_idx, 0],
        X_3d[row_idx, 1],
        X_3d[row_idx, 2],
        c=[session_color_map[row["sessionId"]]],
        marker=behavior_marker_map[b],
        s=45,
        alpha=0.7,
        edgecolors="black",
        linewidth=0.3,
        picker=True,
    )
    point_targets[scatter] = row_idx

# Cluster centers
ax.scatter(
    centroids_pca[:, 0],
    centroids_pca[:, 1],
    centroids_pca[:, 2],
    c="black",
    s=200,
    marker="X",
    label="Cluster Centers",
)

ax.set_xlabel(f"PC1 ({explained_variance[0]*100:.1f}%)")
ax.set_ylabel(f"PC2 ({explained_variance[1]*100:.1f}%)")
ax.set_zlabel(f"PC3 ({explained_variance[2]*100:.1f}%)")

ax.set_title(
    "Telemetry Behaviour Clustering (PCA Projection)\n"
    "Color = Session | Shape = Behavior Configuration | Click a point to open replay"
)

# ==========================
# RIGHT PANEL UI
# ==========================
click_targets = {}  # artist -> payload
missing_icons = []

# ---- Behaviors header
panel.text(
    0.02,
    0.98,
    "Behaviors (click to see map)",
    transform=panel.transAxes,
    va="top",
    fontsize=11,
    weight="bold",
)

# Two-column behavior layout
col1_x_marker, col1_x_text = 0.02, 0.08
col2_x_marker, col2_x_text = 0.52, 0.58

row_h_beh = 0.17
top_beh = 0.93

# Leave room for sessions + PCA text
behaviors_bottom_cutoff = 0.44

for i, b in enumerate(unique_behaviors):
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
        marker=behavior_marker_map[b],
        color="lightgray",
        edgecolors="black",
        linewidths=0.6,
        zorder=3,
    )

    icon_path = behavior_to_icon_path(b)

    label = format_behavior_rows(b)
    if icon_path is None:
        missing_icons.append(b)
        label = label + "\n(no icon)"

    t = panel.text(
        x_text,
        y,
        label,
        transform=panel.transAxes,
        va="top",
        fontsize=9,
        linespacing=1.15,
    )
    t.set_picker(True)
    click_targets[t] = ("__behavior__", b, icon_path)

# ==========================
# Sessions section (collapsible)
# ==========================
sessions_expanded = True

sessions_header_y = 0.34
sessions_top_y = 0.30
row_h_sess = 0.042

sessions_header = panel.text(
    0.02,
    sessions_header_y,
    "▼ Sessions (click to collapse)",
    transform=panel.transAxes,
    va="center",
    fontsize=11,
    weight="bold",
)
sessions_header.set_picker(True)
click_targets[sessions_header] = ("__toggle_sessions__",)

session_row_artists = []

for j, s in enumerate(unique_sessions):
    yy = sessions_top_y - j * row_h_sess
    if yy < 0.16:
        break

    dot = panel.scatter(
        0.04,
        yy,
        transform=panel.transAxes,
        s=70,
        marker="o",
        color=session_color_map[s],
        edgecolors="black",
        linewidths=0.6,
        zorder=3,
    )
    txt = panel.text(
        0.10,
        yy,
        str(s),
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
        "▼ Sessions (click to collapse)" if sessions_expanded else "► Sessions (click to expand)"
    )
    fig.canvas.draw_idle()


# ==========================
# PCA INTERPRETATION TEXT BOX (panel bottom)
# ==========================
interpretation_text = (
    "PCA Dimension Interpretation\n\n"
    f"PC1 ({explained_variance[0]*100:.1f}% variance)\n"
    f"{pc1_text}\n\n"
    f"PC2 ({explained_variance[1]*100:.1f}% variance)\n"
    f"{pc2_text}\n\n"
    f"PC3 ({explained_variance[2]*100:.1f}% variance)\n"
    f"{pc3_text}"
)

panel.text(
    0.02,
    0.13,
    interpretation_text,
    transform=panel.transAxes,
    va="top",
    fontsize=9,
    bbox=dict(boxstyle="round,pad=0.5", facecolor="white", edgecolor="gray"),
)

# ==========================
# PICK HANDLER
# ==========================
def on_pick(event):
    global sessions_expanded
    artist = event.artist

    # Right panel UI clicks
    if artist in click_targets:
        payload = click_targets[artist]
        kind = payload[0]

        if kind == "__toggle_sessions__":
            sessions_expanded = not sessions_expanded
            set_sessions_visible(sessions_expanded)
            update_sessions_header()
            return

        if kind == "__behavior__":
            _, b, icon_path = payload
            if icon_path is None:
                return
            open_expanded(icon_path, title=f"Behavior {b}")
            return

    # 3D point clicks
    if artist in point_targets:
        row_idx = point_targets[artist]
        row = df.loc[row_idx]

        behavior = csv_behavior_tuple(row)
        icon_path = behavior_to_icon_path(behavior)
        video_path = find_video_for_row(row)

        open_session_popup(row, icon_path, video_path)

        if video_path:
            print(f"Opening replay: {video_path}")
            open_video_file(video_path)
        else:
            print(
                "No matching replay found for "
                f"sessionId={row['sessionId']}, "
                f"behavior={behavior_hash8(behavior)}, "
                f"date={row.get('match_date', None)}, "
                f"minute={row.get('match_minute', None)}"
            )


fig.canvas.mpl_connect("pick_event", on_pick)

set_sessions_visible(sessions_expanded)
update_sessions_header()

# ==========================
# DEBUG / INFO
# ==========================
video_root = Path(VIDEO_ROOT_DIR)
if not video_root.exists():
    print(f"\nWARNING: VIDEO_ROOT_DIR does not exist: {video_root.resolve() if video_root else VIDEO_ROOT_DIR}")
else:
    count_videos = sum(1 for _ in iter_video_files(VIDEO_ROOT_DIR))
    print(f"\nVideo search root: {video_root.resolve()}")
    print(f"Found {count_videos} candidate video files recursively.")

if missing_icons:
    print("\nWARNING: Missing behavior icons for these behavior tuples:")
    for b in missing_icons:
        expected = os.path.join(ICON_DIR, f"behavior_{behavior_hash8(b)}.png")
        print(f"  {b} -> expected something like: {expected}")

plt.show()