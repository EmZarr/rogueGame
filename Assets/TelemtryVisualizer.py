# TelemtryVisualizer.py
# ------------------------------------------------------------
# Matplotlib plot with a custom right-side panel:
# - KMeans clustering + PCA
# - Color per sessionId (deterministic)
# - Marker shape per behavior combination
# - Right panel has TWO sections:
#     1) Behaviors (2 columns): marker shape + named rows; click text to expand icon
#     2) Sessions (collapsible): click header to expand/collapse sessionId -> color list
#
# Icons:
# - Put behavior icons in ICON_DIR:
#     icons/behavior_<hash8>.png (or .jpg/.jpeg/.webp)
# - hash8 = md5(repr(behavior_tuple))[:8]
# ------------------------------------------------------------

import os
import hashlib
import numpy as np
import pandas as pd
import matplotlib.pyplot as plt
import matplotlib.image as mpimg

from sklearn.cluster import KMeans
from sklearn.decomposition import PCA
from sklearn.preprocessing import StandardScaler


# ==========================
# CONFIG
# ==========================
CSV_PATH = "Telemetry_Raw.csv"
N_CLUSTERS = 5

ICON_DIR = "icons"
PANEL_WIDTH = 0.33  # fraction of figure width reserved for the right panel

behavior_columns = [
    "GeometryBehavior",
    "FurnishingBehaviorSpread",
    "FurnishingBehaviorRatio",
    "EnemyBehaviorRatio",
    "EnemyBehaviorDifficulty",
]

# NOTE: This maps the tuple positions you DISPLAY.
# Your behavior tuple is (geo_x, geo_y, furn_x, furn_y, enem_x, enem_y)
# but geo_y is forced 0. If you don't want to display it, keep it omitted here.
BEHAVIOR_FIELDS = [
    ("Room Amount Tier", 0, 10),
    ("Loot on Main Ratio", 2, 5),
    ("Loot Health Ratio", 3, 5),
    ("Enemy Encounter Type", 4, 126),
    ("Difficulty", 5, 5),
]

MARKERS = ["o", "s", "^", "D", "P", "X", "*", "v", "<", ">"]


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


# ==========================
# LOAD DATA
# ==========================
df = pd.read_csv(CSV_PATH)

if "sessionId" not in df.columns:
    raise ValueError("CSV must contain a 'sessionId' column.")

missing_beh = [c for c in behavior_columns if c not in df.columns]
if missing_beh:
    raise ValueError(f"Missing behavior columns in CSV: {missing_beh}")


def csv_behavior_tuple(row) -> tuple:
    geo_x = int(row["GeometryBehavior"])
    geo_y = 0  # matches forced geo_y in icon generator
    furn_x = int(row["FurnishingBehaviorSpread"])
    furn_y = int(row["FurnishingBehaviorRatio"])
    enem_x = int(row["EnemyBehaviorRatio"])
    enem_y = int(row["EnemyBehaviorDifficulty"])
    return (geo_x, geo_y, furn_x, furn_y, enem_x, enem_y)


behavior_combinations = df.apply(csv_behavior_tuple, axis=1)
unique_behaviors = behavior_combinations.unique()

# ==========================
# SELECT FEATURES (NUMERIC ONLY)
# ==========================
excluded = ["sessionId"] + behavior_columns
feature_df = df.drop(columns=excluded, errors="ignore").select_dtypes(include=[np.number])

if feature_df.shape[1] == 0:
    raise ValueError(
        "No numeric feature columns found after excluding sessionId + behavior columns. "
        "If you have timestamps/strings, they must be excluded or converted."
    )

X = feature_df.to_numpy()
print(f"Using {feature_df.shape[1]} numeric feature columns for clustering.")

# ==========================
# SCALE / CLUSTER / PCA
# ==========================
scaler = StandardScaler()
X_scaled = scaler.fit_transform(X)

kmeans = KMeans(n_clusters=N_CLUSTERS, random_state=42)
clusters = kmeans.fit_predict(X_scaled)
df["Cluster"] = clusters

pca = PCA(n_components=2, random_state=42)
X_2d = pca.fit_transform(X_scaled)

# ==========================
# COLOR + SHAPE MAPPING
# ==========================
unique_sessions = df["sessionId"].unique()
session_color_map = {s: session_to_color(s) for s in unique_sessions}
colors = df["sessionId"].map(session_color_map).to_numpy()

# marker assignment depends on first-seen order of unique behaviors in the CSV
behavior_marker_map = {b: MARKERS[i % len(MARKERS)] for i, b in enumerate(unique_behaviors)}

# ==========================
# FIGURE LAYOUT
# ==========================
fig = plt.figure(figsize=(14, 8))

# Main plot axis (left)
ax = fig.add_axes([0.07, 0.10, 0.88 - PANEL_WIDTH, 0.83])

# Right panel axis
panel = fig.add_axes([0.07 + (0.88 - PANEL_WIDTH) + 0.02, 0.10, PANEL_WIDTH - 0.04, 0.83])
panel.set_axis_off()

# ==========================
# MAIN SCATTER PLOT
# ==========================
for b in unique_behaviors:
    idx = (behavior_combinations == b).to_numpy()
    ax.scatter(
        X_2d[idx, 0],
        X_2d[idx, 1],
        c=list(colors[idx]),
        marker=behavior_marker_map[b],
        alpha=0.7,
        edgecolors="k",
        linewidths=0.6,
    )

ax.set_title("Telemetry Clustering (Color=SessionID, Shape=Behavior Combo)")
ax.set_xlabel("PCA Component 1")
ax.set_ylabel("PCA Component 2")
ax.grid(True)

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

# ---- Two-column behavior layout
# Column geometry (in axes fraction)
col1_x_marker, col1_x_text = 0.02, 0.08
col2_x_marker, col2_x_text = 0.52, 0.58

# Each behavior card is multiline, so step with a larger row height.
row_h_beh = 0.17
top_beh = 0.93

# Reserve space below behaviors for sessions; tweak this to push sessions further down.
# (Higher cutoff => more room for sessions; lower cutoff => more room for behaviors)
behaviors_bottom_cutoff = 0.40

for i, b in enumerate(unique_behaviors):
    col = i % 2
    row = i // 2

    x_marker = col1_x_marker if col == 0 else col2_x_marker
    x_text = col1_x_text if col == 0 else col2_x_text

    y = top_beh - row * row_h_beh
    if y < behaviors_bottom_cutoff:
        break

    # marker sample aligned to the top of the behavior block
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

    # Clickable text block
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
# Sessions section (moved down + collapsible)
# ==========================
sessions_expanded = True

# Push sessions down by lowering header_y/top_y; adjust to taste.
sessions_header_y = 0.30
sessions_top_y = 0.26
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
    if yy < 0.02:
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


def on_pick(event):
    global sessions_expanded
    artist = event.artist
    if artist not in click_targets:
        return

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


fig.canvas.mpl_connect("pick_event", on_pick)

set_sessions_visible(sessions_expanded)
update_sessions_header()

# ==========================
# OPTIONAL: PRINT MISSING ICONS
# ==========================
if missing_icons:
    print("\nWARNING: Missing behavior icons for these behavior tuples:")
    for b in missing_icons:
        expected = os.path.join(ICON_DIR, f"behavior_{behavior_hash8(b)}.png")
        print(f"  {b} -> expected something like: {expected}")

plt.show()