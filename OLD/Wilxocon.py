import pandas as pd
import numpy as np
from scipy.stats import wilcoxon
from itertools import combinations

# -------------------------
# CONFIG
# -------------------------

df = pd.read_csv("Telemetry_Raw.csv")

player_col = "playerId"

level_features = [
    "GeometryBehavior",
    "FurnishingBehaviorSpread",
    "FurnishingBehaviorRatio",
    "EnemyBehaviorRatio",
    "EnemyBehaviorDifficulty"
]

exclude_cols = [
    "timestamp",
    "levelPlayID",
    "TotalScore",
    "deaths",
    "timePlayed"
]

# Determine telemetry features automatically
telemetry_features = [
    c for c in df.columns
    if c not in exclude_cols + level_features
]

# -------------------------
# CREATE LEVEL CONFIGURATION ID
# -------------------------

df["LevelConfig"] = df[level_features].astype(str).agg("_".join, axis=1)

# -------------------------
# HANDLE MULTIPLE PLAYS
# -------------------------

player_level = (
    df.groupby([player_col, "LevelConfig"])[telemetry_features]
    .mean()
    .reset_index()
)

# -------------------------
# RUN WILCOXON TESTS
# -------------------------

results = []

configs = player_level["LevelConfig"].unique()

for c1, c2 in combinations(configs, 2):

    d1 = player_level[player_level["LevelConfig"] == c1]
    d2 = player_level[player_level["LevelConfig"] == c2]

    merged = pd.merge(
        d1,
        d2,
        on=player_col,
        suffixes=("_lvl1", "_lvl2")
    )

    if len(merged) < 5:
        continue

    for feature in telemetry_features:

        x = merged[f"{feature}_lvl1"]
        y = merged[f"{feature}_lvl2"]

        if np.allclose(x, y):
            continue

        try:
            stat, p = wilcoxon(x, y)

            results.append({
                "Feature": feature,
                "Config1": c1,
                "Config2": c2,
                "PlayersCompared": len(merged),
                "Statistic": stat,
                "p_value": p
            })

        except:
            continue

results_df = pd.DataFrame(results).sort_values("p_value")

print(results_df.head(20))