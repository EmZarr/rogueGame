import pandas as pd
import numpy as np
import matplotlib.pyplot as plt
import matplotlib.lines as mlines
from mpl_toolkits.mplot3d import Axes3D
from sklearn.cluster import KMeans
from sklearn.decomposition import PCA
from sklearn.preprocessing import StandardScaler
from scipy.spatial import ConvexHull
import hashlib

# ==========================
# CONFIG
# ==========================
CSV_PATH = "Telemetry_Raw.csv"
N_CLUSTERS = 5

# ==========================
# LOAD DATA
# ==========================
df = pd.read_csv(CSV_PATH)

df = df.drop(columns=["timestamp"])

feature_columns = [c for c in df.columns if c != "sessionId"]

X = df[feature_columns].values

# ==========================
# SCALE
# ==========================
scaler = StandardScaler()
X_scaled = scaler.fit_transform(X)

# ==========================
# KMEANS
# ==========================
kmeans = KMeans(n_clusters=N_CLUSTERS, random_state=42)
clusters = kmeans.fit_predict(X_scaled)

# ==========================
# PCA
# ==========================
pca = PCA(n_components=3)
X_3d = pca.fit_transform(X_scaled)

explained_variance = pca.explained_variance_ratio_

print("\nExplained Variance Ratio:")
for i, var in enumerate(explained_variance):
    print(f"PC{i+1}: {var:.4f} ({var*100:.2f}%)")

print("\nTotal variance captured:", sum(explained_variance)*100, "%")

# ==========================
# PCA COMPONENT WEIGHTS
# ==========================
loadings = pd.DataFrame(
    pca.components_.T,
    columns=["PC1", "PC2", "PC3"],
    index=feature_columns
)

for pc in ["PC1", "PC2", "PC3"]:
    print(f"\nTop contributing features for {pc}:")
    sorted_features = loadings[pc].abs().sort_values(ascending=False)
    for feature in sorted_features.head(10).index:
        weight = loadings.loc[feature, pc]
        print(f"{feature}: {weight:.4f}")

# ==========================
# SESSION COLOR MAP
# ==========================
unique_sessions = sorted(df["sessionId"].unique())

def session_to_color(session_id):
    hash_val = int(hashlib.md5(session_id.encode()).hexdigest(), 16)
    np.random.seed(hash_val % (2**32))
    return np.random.rand(3)

session_color_map = {s: session_to_color(s) for s in unique_sessions}
colors = df["sessionId"].map(session_color_map)

# ==========================
# BEHAVIOR COMBINATIONS
# ==========================
behavior_columns = [
    "GeometryBehavior",
    "FurnishingBehaviorSpread",
    "FurnishingBehaviorRatio",
    "EnemyBehaviorRatio",
    "EnemyBehaviorDifficulty"
]

df[behavior_columns] = df[behavior_columns].round(2)

behavior_combos = df[behavior_columns].apply(tuple, axis=1)
unique_behaviors = behavior_combos.unique()

markers = ['o','s','^','D','P','X','*','v','<','>']

behavior_marker_map = {
    b: markers[i % len(markers)]
    for i, b in enumerate(unique_behaviors)
}

# readable labels
behavior_labels = {
    b: (
        f"G={b[0]} | FS={b[1]} | FR={b[2]} | "
        f"ER={b[3]} | ED={b[4]}"
    )
    for b in unique_behaviors
}

# ==========================
# CLUSTER CENTROIDS (PCA SPACE)
# ==========================
centroids_scaled = kmeans.cluster_centers_
centroids_pca = pca.transform(centroids_scaled)



# ==========================
# CLUSTER SUMMARIES
# ==========================

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
        val = cluster_means.loc[c,feature]
        delta = diffs.loc[feature]

        direction = "higher" if delta > 0 else "lower"

        print(f"  {feature}: {val:.3f} ({direction} than average)")

# ==========================
# PCA INTERPRETATION
# ==========================

print("\n==============================")
print("PCA DIMENSION INTERPRETATION")
print("==============================")

for pc in ["PC1","PC2","PC3"]:

    print(f"\n{pc} represents:")

    sorted_features = loadings[pc].sort_values(key=lambda x: abs(x), ascending=False)

    for f in sorted_features.head(6).index:

        weight = loadings.loc[f,pc]

        direction = "increases with" if weight > 0 else "decreases with"

        print(f"  {direction} {f} ({weight:.3f})")

# ==========================
# PCA INTERPRETATION TEXT
# ==========================

def build_pc_interpretation(pc_name):

    sorted_features = loadings[pc_name].sort_values(
        key=lambda x: abs(x), ascending=False
    )

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
# 3D PLOT
# ==========================
fig = plt.figure(figsize=(16,10))
ax = fig.add_subplot(111, projection='3d')

# ==========================
# CLUSTER HULLS
# ==========================

cluster_colors = plt.cm.tab10(np.linspace(0,1,N_CLUSTERS))

for i in range(N_CLUSTERS):

    points = X_3d[clusters == i]

    if len(points) < 4:
        continue

    hull = ConvexHull(points)

    for simplex in hull.simplices:
        ax.plot(
            points[simplex,0],
            points[simplex,1],
            points[simplex,2],
            color=cluster_colors[i],
            alpha=0.25
        )

# scatter points
for behavior in unique_behaviors:
    idx = behavior_combos == behavior
    
    ax.scatter(
        X_3d[idx,0],
        X_3d[idx,1],
        X_3d[idx,2],
        c=list(colors[idx]),
        marker=behavior_marker_map[behavior],
        s=45,
        alpha=0.7,
        edgecolors="black",
        linewidth=0.3
    )

# plot cluster centers
ax.scatter(
    centroids_pca[:,0],
    centroids_pca[:,1],
    centroids_pca[:,2],
    c="black",
    s=200,
    marker="X",
    label="Cluster Centers"
)

ax.set_xlabel(f"PC1 ({explained_variance[0]*100:.1f}%)")
ax.set_ylabel(f"PC2 ({explained_variance[1]*100:.1f}%)")
ax.set_zlabel(f"PC3 ({explained_variance[2]*100:.1f}%)")

plt.title(
    "Telemetry Behaviour Clustering (PCA Projection)\n"
    "Color = Session | Shape = Behavior Configuration"
)

# ==========================
# SESSION LEGEND
# ==========================
session_handles = []

for session, color in session_color_map.items():
    handle = mlines.Line2D(
        [],
        [],
        color=color,
        marker='o',
        linestyle='None',
        markersize=8,
        label=session
    )
    session_handles.append(handle)

session_legend = ax.legend(
    handles=session_handles,
    title="Session ID (Color)",
    loc="upper left",
    bbox_to_anchor=(1.02,1)
)

ax.add_artist(session_legend)

# ==========================
# BEHAVIOR LEGEND
# ==========================
behavior_handles = []

for behavior, marker in behavior_marker_map.items():

    handle = mlines.Line2D(
        [],
        [],
        color='black',
        marker=marker,
        linestyle='None',
        markersize=8,
        label=behavior_labels[behavior]
    )

    behavior_handles.append(handle)

ax.legend(
    handles=behavior_handles,
    title="Behavior Configuration (Shape)",
    loc="lower left",
    bbox_to_anchor=(1.02,0)
)

plt.subplots_adjust(right=0.65)

# ==========================
# PCA INTERPRETATION PANEL
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

fig.text(
    0.72,
    0.55,
    interpretation_text,
    fontsize=10,
    verticalalignment='top',
    bbox=dict(
        boxstyle="round,pad=0.5",
        facecolor="white",
        edgecolor="gray"
    )
)

plt.show()