import pandas as pd
import statsmodels.formula.api as smf

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
    "playerId",
    "TotalScore",
    "deaths",
    "timePlayed"
]

telemetry_features = [
    c for c in df.columns
    if c not in exclude_cols + level_features
]

results = []

# -------------------------
# RUN MODEL FOR EACH TELEMETRY FEATURE
# -------------------------

for feature in telemetry_features:

    try:

        formula = (
            f"{feature} ~ "
            + " + ".join(level_features)
        )

        model = smf.mixedlm(
            formula,
            df,
            groups=df[player_col]
        )

        fit = model.fit()

        for param in level_features:

            coef = fit.params.get(param, None)
            pval = fit.pvalues.get(param, None)

            results.append({
                "TelemetryFeature": feature,
                "LevelParameter": param,
                "Coefficient": coef,
                "p_value": pval
            })

    except:
        continue

results_df = pd.DataFrame(results)
results_df = results_df.sort_values("p_value")

print(results_df.head(30))