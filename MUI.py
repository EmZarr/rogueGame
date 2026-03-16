import pandas as pd
import numpy as np
from sklearn.feature_selection import mutual_info_regression
from sklearn.preprocessing import StandardScaler

# ---- CONFIG ----
INPUT_FILE = "Telemetry_Raw.csv"

TARGET_FEATURES = [
    "GeometryBehavior",
    "FurnishingBehaviorSpread",
    "FurnishingBehaviorRatio",
    "EnemyBehaviorRatio",
    "EnemyBehaviorDifficulty"
]

# Columns to ignore completely
IGNORE_COLUMNS = [
    "timestamp",
    "levelPlayID",
    "playerId",
    "TotalScore",
    "deaths",
    "timePlayed"
]

# ----------------

def compute_joint_mutual_information(df, target_features):

    df = df.dropna()

    # Joint parameter space
    X = df[target_features]

    scaler = StandardScaler()
    X_scaled = scaler.fit_transform(X)

    results = []

    for column in df.columns:

        # Skip target parameters
        if column in target_features:
            continue

        # Skip ignored columns
        if column in IGNORE_COLUMNS:
            continue

        y = df[column]

        # Skip non-numeric telemetry
        if not np.issubdtype(y.dtype, np.number):
            continue

        mi = mutual_info_regression(X_scaled, y, random_state=42)

        # Combine MI contributions from the 5 parameters
        joint_mi = np.sum(mi)

        results.append((column, joint_mi))

    results.sort(key=lambda x: x[1], reverse=True)

    return pd.DataFrame(results, columns=["TelemetryFeature", "MutualInformation"])


def main():

    df = pd.read_csv(INPUT_FILE)

    results = compute_joint_mutual_information(df, TARGET_FEATURES)

    print("\nTop correlations with parameter combination:\n")
    print(results.head(40))

    results.to_csv("mutual_information_results.csv", index=False)


if __name__ == "__main__":
    main()