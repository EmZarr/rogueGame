import pandas as pd
import numpy as np
from scipy.stats import kruskal

# ---- CONFIG ----
INPUT_FILE = "Telemetry_Raw.csv"

TARGET_FEATURES = [
    "GeometryBehavior",
    "FurnishingBehaviorSpread",
    "FurnishingBehaviorRatio",
    "EnemyBehaviorRatio",
    "EnemyBehaviorDifficulty"
]

IGNORE_COLUMNS = [
    "timestamp",
    "levelPlayID",
    "playerId",
    "TotalScore",
    "deaths",
    "timePlayed"
]
# ----------------


def kruskal_analysis(df):

    df = df.dropna()

    # Create group identifier from the 5 parameters
    df["param_group"] = df[TARGET_FEATURES].astype(str).agg("_".join, axis=1)

    results = []

    for column in df.columns:

        if column in TARGET_FEATURES:
            continue

        if column in IGNORE_COLUMNS:
            continue

        if column == "param_group":
            continue

        y = df[column]

        # Skip non-numeric telemetry
        if not np.issubdtype(y.dtype, np.number):
            continue

        # Build samples for each parameter group
        groups = [
            group[column].values
            for _, group in df.groupby("param_group")
            if len(group) > 1
        ]

        # Need at least 2 groups
        if len(groups) < 2:
            continue

        try:
            H, p = kruskal(*groups)
            results.append((column, H, p))
        except:
            continue

    results_df = pd.DataFrame(
        results,
        columns=["TelemetryFeature", "H_statistic", "p_value"]
    )

    results_df = results_df.sort_values("p_value")

    return results_df


def main():

    df = pd.read_csv(INPUT_FILE)

    results = kruskal_analysis(df)

    print("\nTelemetry variables most affected by parameter combinations:\n")
    print(results.head(20))

    results.to_csv("kruskal_results.csv", index=False)


if __name__ == "__main__":
    main()