# TelemtryVisualizer.py
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
from scipy.spatial import ConvexHull


# ==========================
# CONFIG
# ==========================
CSV_PATH = "Telemetry_Raw.csv"
N_CLUSTERS = 5

ICON_DIR = "icons"
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


# ==========================
# LOAD DATA
# ==========================
df = pd.read_csv(CSV_PATH)

if "sessionId" not in df.columns:
    raise ValueError("CSV must contain a 'sessionId' column.")

missing_beh = [c for c in behavior_columns if c not in df.columns]
if missing_beh:
    raise ValueError(f"Missing behavior columns in CSV: {missing_beh}")

# Drop timestamp if present (your 'main' branch did this)
df = df.drop(columns=["timestamp"], errors="ignore")

# Build behavior tuples per row (used for markers + icons)
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
colors = df["sessionId"].map(session_color_map).to_numpy()

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
# MAIN 3D SCATTER
# ==========================
for b in unique_behaviors:
    idx = (behavior_combinations == b).to_numpy()
    ax.scatter(
        X_3d[idx, 0],
        X_3d[idx, 1],
        X_3d[idx, 2],
        c=list(colors[idx]),
        marker=behavior_marker_map[b],
        s=45,
        alpha=0.7,
        edgecolors="black",
        linewidth=0.3,
    )

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
    "Color = Session | Shape = Behavior Configuration"
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