import pandas as pd
import numpy as np
from sklearn.preprocessing import StandardScaler
from sklearn.decomposition import PCA
import matplotlib.pyplot as plt
import seaborn as sns

# Archetypal Analysis library
from archetypes import AA


# -----------------------------
# PLAYER FILTERING OPTIONS
# -----------------------------

# Option A: exclude specific players
EXCLUDE_PLAYERS = [
    "Fabio",
    "Nicholas",
    "Benjamin"
]

# Option B: include ONLY these players (overrides exclude if not empty)
INCLUDE_ONLY_PLAYERS = [
    # "player_id_3",
]

# Toggle filtering
ENABLE_PLAYER_FILTER = True
# -----------------------------
# CONFIG
# -----------------------------
INPUT_FILE = "TelemetryExplore.csv"
N_ARCHETYPES = 4  # tune this (3–6 usually good)

CLUSTER_FEATURES = [
    "POTakePct",
    "HPTakePct",
    "AvgEneAliveOnPOTakePct",
    "OptionalRoomPercentage",
    "AverageDistanceToMainPath"
]

LEVEL_FEATURES = [
    "GeometryBehavior",
    "FurnishingBehaviorSpread",
    "FurnishingBehaviorRatio",
    "EnemyBehaviorRatio",
    "EnemyBehaviorDifficulty"
]

PLAYER_ID_COL = "playerId"

# -----------------------------
# LOAD DATA
# -----------------------------
df = pd.read_csv(INPUT_FILE)
df = df.dropna(subset=CLUSTER_FEATURES + LEVEL_FEATURES + [PLAYER_ID_COL])

# -----------------------------
# SCALE DATA
# -----------------------------
scaler = StandardScaler()

# -----------------------------
# APPLY PLAYER FILTER
# -----------------------------
if ENABLE_PLAYER_FILTER:

    if INCLUDE_ONLY_PLAYERS:
        df = df[df[PLAYER_ID_COL].isin(INCLUDE_ONLY_PLAYERS)]
        print(f"Using ONLY {len(INCLUDE_ONLY_PLAYERS)} players")

    elif EXCLUDE_PLAYERS:
        df = df[~df[PLAYER_ID_COL].isin(EXCLUDE_PLAYERS)]
        print(f"Excluding {len(EXCLUDE_PLAYERS)} players")

print(f"Remaining rows after filtering: {len(df)}")

X_scaled = scaler.fit_transform(df[CLUSTER_FEATURES])

# -----------------------------
# ARCHETYPAL ANALYSIS
# -----------------------------
aa = AA(n_archetypes=N_ARCHETYPES, random_state=42)
aa.fit(X_scaled)

# Each row = mixture of archetypes
weights = aa.A_  # shape: (n_samples, n_archetypes)

# Assign dominant archetype for visualization
df["Archetype"] = np.argmax(weights, axis=1)

# -----------------------------
# PCA FOR VISUALIZATION
# -----------------------------
pca = PCA(n_components=2)
X_pca = pca.fit_transform(X_scaled)

df["PCA1"] = X_pca[:, 0]
df["PCA2"] = X_pca[:, 1]

# -----------------------------
# LEVEL COLORS
# -----------------------------
df["LevelID"] = df[LEVEL_FEATURES].astype(str).agg("_".join, axis=1)
unique_levels = df["LevelID"].unique()

palette = sns.color_palette("tab20", len(unique_levels))
level_color_map = dict(zip(unique_levels, palette))

# -----------------------------
# PLAYER SHAPES
# -----------------------------
markers = ['o', 's', '^', 'D', 'P', '*', 'X', '<', '>', 'v']
unique_players = df[PLAYER_ID_COL].unique()

player_marker_map = {
    player: markers[i % len(markers)]
    for i, player in enumerate(unique_players)
}

# -----------------------------
# FIGURE WITH SIDE PANEL
# -----------------------------
fig = plt.figure(figsize=(16, 9))
gs = fig.add_gridspec(1, 2, width_ratios=[3, 1])

ax = fig.add_subplot(gs[0])
info_ax = fig.add_subplot(gs[1])
info_ax.axis('off')

# -----------------------------
# SCATTER PLOT
# -----------------------------
for _, row in df.iterrows():
    ax.scatter(
        row["PCA1"],
        row["PCA2"],
        color=level_color_map[row["LevelID"]],
        marker=player_marker_map[row[PLAYER_ID_COL]],
        s=80,
        edgecolor='black',
        linewidth=1 + row["Archetype"] * 0.3,  # subtle archetype hint
        alpha=0.8
    )

# -----------------------------
# LEGENDS
# -----------------------------
from matplotlib.lines import Line2D

level_legend = [
    Line2D([0], [0], marker='o', color='w',
           markerfacecolor=color, markersize=8,
           label=f"Level {i}")
    for i, (lvl, color) in enumerate(level_color_map.items())
]

player_legend = [
    Line2D([0], [0], marker=marker, color='black',
           linestyle='None', markersize=8,
           label=str(player)[:6])
    for player, marker in player_marker_map.items()
]

ax.legend(handles=level_legend, title="Levels", loc='upper right')
ax.add_artist(ax.legend(handles=player_legend, title="Players", loc='lower right'))

# -----------------------------
# INFO PANEL (RIGHT SIDE)
# -----------------------------
df = df.reset_index(drop=True)

sample_df = df.sample(min(12, len(df)), random_state=42)

info_text = "Sample Points (AA):\n\n"



for i, row in sample_df.iterrows():
    level_info = ", ".join([f"{lf}:{row[lf]}" for lf in LEVEL_FEATURES])
    archetype_mix = ", ".join([f"A{i}:{w:.2f}" for i, w in enumerate(weights[i])])

    info_text += (
        f"P:{str(row[PLAYER_ID_COL])[:6]}\n"
        f"{level_info}\n"
        f"{archetype_mix}\n\n"
    )

info_ax.text(0, 1, info_text, fontsize=9, va='top')

# -----------------------------
# FINAL TOUCHES
# -----------------------------
ax.set_title("Behavior Space (PCA) with Archetypal Analysis")
ax.set_xlabel("PCA 1")
ax.set_ylabel("PCA 2")
ax.grid(True)

plt.tight_layout()
plt.show()

# -----------------------------
# SAVE OUTPUT
# -----------------------------
df.to_csv("archetypal_analysis_output.csv", index=False)
print("Saved to archetypal_analysis_output.csv")