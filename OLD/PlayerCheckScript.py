import pandas as pd

# -----------------------------
# CONFIG
# -----------------------------
file_path = "TelemetryExplore.csv"  # dataset path
player_id = "06ff48572547bf12bd40561cc5d6f926c19fcaf4"      # player to analyze
metric = "POTakePct"         # telemetry parameter

level_features = [
    "GeometryBehavior",
    "FurnishingBehaviorSpread",
    "FurnishingBehaviorRatio",
    "EnemyBehaviorRatio",
    "EnemyBehaviorDifficulty"
]

player_column = "playerId"


# -----------------------------
# LOAD DATA
# -----------------------------
df = pd.read_csv(file_path)

# Filter player
player_df = df[df[player_column] == player_id]

if player_df.empty:
    raise ValueError(f"No data found for player {player_id}")

# -----------------------------
# GROUP BY LEVEL FEATURES
# -----------------------------
results = []
level_variances = []

for level_key, group in player_df.groupby(level_features):

    # Add all individual playthrough rows
    for _, row in group.iterrows():
        entry = {feature: row[feature] for feature in level_features}
        entry["Playthrough"] = "Individual"
        entry[metric] = row[metric]
        results.append(entry)

    # Compute aggregates
    avg_score = group[metric].mean()
    
    # Compute "average variance" instead of the standard variance
    variance_score = ((group[metric] - avg_score).abs()).mean()  # mean absolute deviation
    # OR, if you want the average of squared differences (standard variance)
    # variance_score = ((group[metric] - avg_score) ** 2).mean()  

    level_variances.append(variance_score)

    agg_entry = {feature: val for feature, val in zip(level_features, level_key)}
    agg_entry["Playthrough"] = "Aggregate"
    agg_entry[f"{metric}_avg"] = avg_score
    agg_entry[f"{metric}_variance"] = variance_score

    results.append(agg_entry)


# -----------------------------
# COMPUTE TOTAL PLAYER AVERAGE
# -----------------------------
total_avg = player_df[metric].mean()
total_avg_variance = pd.Series(level_variances).mean()  # mean of per-level variances

total_entry = {feature: "ALL" for feature in level_features}
total_entry["Playthrough"] = "Total"
total_entry[f"{metric}_avg"] = total_avg
total_entry[f"{metric}_variance"] = total_avg_variance
results.append(total_entry)


# -----------------------------
# OUTPUT RESULT
# -----------------------------
result_df = pd.DataFrame(results)

print(result_df)

# Optional: save to CSV
result_df.to_csv(f"player_{player_id}_{metric}_level_analysis.csv", index=False)