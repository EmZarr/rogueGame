import pandas as pd
import matplotlib.pyplot as plt
import seaborn as sns
from sklearn.preprocessing import StandardScaler
from sklearn.decomposition import PCA
from sklearn.cluster import KMeans

sns.set(style="whitegrid")

# =====================================================
# LOAD DATA
# =====================================================

df = pd.read_csv("Telemetry_Raw.csv")

# Rename columns to match your new telemetry format if needed
df = df.rename(columns={
    "sessionId": "session_id",
    "timePlayed": "time_played",
    "enemiesKilledPct": "enemies_killed_pct",
    "lootTakenPct": "loot_taken_pct",
    "bowmanTime": "bowman_time",
    "knightTime": "knight_time",
    "berserkerTime": "berserker_time"
})

# =====================================================
# FEATURE VECTOR (ALL NUMERIC TELEMETRY)
# =====================================================

feature_columns = [
    "time_played",
    "enemies_killed_pct",
    "loot_taken_pct",
    "deaths",

    "bowman_time",
    "knight_time",
    "berserker_time",

    "bowLightAtk", "bowHeavyAtk", "bowLightDash", "bowHeavyDash", "bowDefense",
    "knightLightAtk", "knightHeavyAtk", "knightLightDash", "knightHeavyDash", "knightDefense",
    "berserkLightAtk", "beserkHeavyAtk", "beserkLightDash", "beserkHeavyDash", "beserkDefense",

    "damageTaken[0]", "damageTaken[1]", "damageTaken[2]", "damageTaken[3]"
]

# Keep only existing columns (safety)
feature_columns = [col for col in feature_columns if col in df.columns]

X = df[feature_columns].fillna(0)

# =====================================================
# SCALE FEATURES
# =====================================================

scaler = StandardScaler()
X_scaled = scaler.fit_transform(X)

# =====================================================
# PCA → REDUCE TO 2D FOR VISUALIZATION
# =====================================================

pca = PCA(n_components=2)
X_pca = pca.fit_transform(X_scaled)

# =====================================================
# KMEANS CLUSTERING
# =====================================================

kmeans = KMeans(n_clusters=3, random_state=42, n_init=10)
clusters = kmeans.fit_predict(X_scaled)

df["cluster"] = clusters

# =====================================================
# GRAPH 1 — SESSION VECTOR CLUSTERS (PCA)
# =====================================================

fig1, ax1 = plt.subplots()

scatter = ax1.scatter(
    X_pca[:, 0],
    X_pca[:, 1],
    c=clusters,
    s=100
)

ax1.set_title("Session Vector Clusters (All Telemetry)")
ax1.set_xlabel("PCA Component 1")
ax1.set_ylabel("PCA Component 2")

# Annotate session IDs
for i, session in enumerate(df["session_id"]):
    ax1.annotate(str(session), (X_pca[i, 0], X_pca[i, 1]), fontsize=8)

# =====================================================
# GRAPH 2 — AVERAGE CLASS TIME DISTRIBUTION (DONUT)
# =====================================================

avg_class_time = df[["bowman_time","knight_time","berserker_time"]].mean()
avg_class_time = avg_class_time[avg_class_time > 0]

fig2, ax2 = plt.subplots()

ax2.pie(
    avg_class_time,
    labels=avg_class_time.index.str.replace("_time", "").str.capitalize(),
    autopct="%1.1f%%",
    startangle=90,
    pctdistance=0.85
)

centre_circle = plt.Circle((0,0),0.70,fc='white')
fig2.gca().add_artist(centre_circle)

ax2.set_title("Average Class Time Distribution Per Session")

# =====================================================
# GRAPH 3 — TIME PLAYED vs LOOT % (CLUSTER COLORED)
# =====================================================

fig3, ax3 = plt.subplots()

sns.scatterplot(
    data=df,
    x="time_played",
    y="loot_taken_pct",
    hue="cluster",
    s=100,
    ax=ax3
)

sns.regplot(
    data=df,
    x="time_played",
    y="loot_taken_pct",
    scatter=False,
    ax=ax3
)

ax3.set_title("Time Played vs Loot Collected (Clustered)")
ax3.set_xlabel("Time Played (seconds)")
ax3.set_ylabel("Loot Collected (%)")

plt.show()