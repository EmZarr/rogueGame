import pandas as pd
import numpy as np
import matplotlib.pyplot as plt
from sklearn.cluster import KMeans
from sklearn.decomposition import PCA
from sklearn.preprocessing import StandardScaler
import hashlib

# ==========================
# CONFIG
# ==========================
CSV_PATH = "Telemetry_Raw.csv"
N_CLUSTERS = 5   # Adjust if needed


# ==========================
# LOAD DATA
# ==========================
df = pd.read_csv(CSV_PATH)

# Remove sessionId from features used for clustering
feature_columns = [col for col in df.columns if col != "sessionId"]

X = df[feature_columns].values

# ==========================
# SCALE FEATURES
# ==========================
scaler = StandardScaler()
X_scaled = scaler.fit_transform(X)

# ==========================
# CLUSTERING
# ==========================
kmeans = KMeans(n_clusters=N_CLUSTERS, random_state=42)
clusters = kmeans.fit_predict(X_scaled)

# ==========================
# PCA FOR 2D VISUALIZATION
# ==========================
pca = PCA(n_components=2)
X_2d = pca.fit_transform(X_scaled)

# ==========================
# COLOR MAPPING (per sessionId)
# ==========================
unique_sessions = df["sessionId"].unique()
session_color_map = {}

# Generate consistent color from hash
def session_to_color(session_id):
    hash_val = int(hashlib.md5(session_id.encode()).hexdigest(), 16)
    np.random.seed(hash_val % (2**32))
    return np.random.rand(3,)

for session in unique_sessions:
    session_color_map[session] = session_to_color(session)

colors = df["sessionId"].map(session_color_map)

# ==========================
# SHAPE MAPPING (per behavior combination)
# ==========================
behavior_columns = [
    "GeometryBehavior",
    "FurnishingBehaviorSpread",
    "FurnishingBehaviorRatio",
    "EnemyBehaviorRatio",
    "EnemyBehaviorDifficulty"
]

# Create a tuple per row to represent behavior combination
behavior_combinations = df[behavior_columns].apply(tuple, axis=1)
unique_behaviors = behavior_combinations.unique()

markers = ['o', 's', '^', 'D', 'P', 'X', '*', 'v', '<', '>']
behavior_marker_map = {}

for i, behavior in enumerate(unique_behaviors):
    behavior_marker_map[behavior] = markers[i % len(markers)]

# ==========================
# PLOT
# ==========================
plt.figure(figsize=(12, 8))

for behavior in unique_behaviors:
    idx = behavior_combinations == behavior
    plt.scatter(
        X_2d[idx, 0],
        X_2d[idx, 1],
        c=list(colors[idx]),
        marker=behavior_marker_map[behavior],
        label=f"Behavior {behavior}",
        alpha=0.7,
        edgecolors='k'
    )

plt.title("Telemetry Clustering (Color=SessionID, Shape=Behavior Combo)")
plt.xlabel("PCA Component 1")
plt.ylabel("PCA Component 2")
plt.grid(True)

# Avoid duplicate legend entries
handles, labels = plt.gca().get_legend_handles_labels()
unique = dict(zip(labels, handles))
plt.legend(unique.values(), unique.keys(), bbox_to_anchor=(1.05, 1), loc='upper left')

plt.tight_layout()
plt.show()