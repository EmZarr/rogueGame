import numpy as np
import pandas as pd


# ============================================================
# CENTRAL CONFIG
# Change values here only.
# ============================================================

CSV_PATH = "Telemetry_RawControl.csv"

FILTER_COLUMN = "GeometryBehavior"
FILTER_VALUES = [1]

INCLUDE_PLAYER_IDS = ["4036b402136b005f97a637e85f7961f4a1df5e23", "a337940f472f2f83c15e80db566145fe9e844d0d", "bbd174147e56b9d0ecff534e0bc4e7aa42920e66", "61ba1ad1e381c2a7ccb892a864aad6a50daf8d96", "86c8cfe04bc19458adcf3c880baccfeb7a03bd7c", "5953ebc01c53dc47066142b416c932f7e8a07982", "e967dc9b7177e4abf497f68b06561fcdd3fb0ebd", "c905b72819f4bbd69818665d6c720bddc85ce1a2"]

PLAYER_ID_COLUMN = "playerId"
LEVEL_PLAY_ID_COLUMN = "levelPlayID"

BEHAVIOR_COLUMNS = [
    "GeometryBehavior",
    "FurnishingBehaviorSpread",
    "FurnishingBehaviorRatio",
    "EnemyBehaviorRatio",
    "EnemyBehaviorDifficulty",
]

NORMALIZATION_METHOD = "zscore"

LEVEL_CONSTANTS_BY_BEHAVIOR = {
    # Level 1
    (7, 2, 1, 85, 2): {"powerups": 8,  "health": 6,  "enemies": 32},

    # Level 2
    (6, 2, 2, 55, 3): {"powerups": 7,  "health": 6,  "enemies": 60},

    # Level 3
    (9, 3, 1, 0, 2): {"powerups": 25, "health": 7,  "enemies": 30},

    # Level 4
    (6, 2, 4, 17, 1): {"powerups": 5,  "health": 21, "enemies": 40},

    # Level 5
    (4, 4, 1, 113, 3): {"powerups": 13, "health": 5,  "enemies": 34},
}


# ============================================================
# HELPERS
# ============================================================

def safe_div(numerator, denominator, default=0.0):
    if pd.isna(denominator) or denominator == 0:
        return default
    if pd.isna(numerator):
        return default
    return float(numerator) / float(denominator)

def get_behavior_key(row):
    return tuple(
        int(row[col]) if not pd.isna(row[col]) else None
        for col in BEHAVIOR_COLUMNS
    )   


def normalize_features(df_features: pd.DataFrame, method: str = "zscore") -> pd.DataFrame:
    result = df_features.copy()

    for col in result.columns:
        series = pd.to_numeric(result[col], errors="coerce").fillna(0.0)

        if method == "zscore":
            mean = series.mean()
            std = series.std(ddof=0)
            result[col] = 0.0 if std == 0 else (series - mean) / std

        elif method == "minmax":
            min_val = series.min()
            max_val = series.max()
            result[col] = 0.0 if max_val == min_val else (series - min_val) / (max_val - min_val)

        else:
            raise ValueError(f"Unknown normalization method: {method}")

    return result


# ============================================================
# CORE FUNCTION
# ============================================================

def process_csv():
    """
    Uses the module config above and returns:
    [
        {
            "features": [...],
            "feature_names": [...],
            "info": {
                "playerId": ...,
                "levelPlayID": ...,
                "behavior5": [...]
            }
        },
        ...
    ]
    """

    df = pd.read_csv(CSV_PATH)

    required_columns = [
        FILTER_COLUMN,
        PLAYER_ID_COLUMN,
        LEVEL_PLAY_ID_COLUMN,
        "PowerUpsTaken",
        "HealthBarrelsTaken",
        "AvgEnemiesAliveOnPowerUpTaken",
        "OptionalRoomPercentage",
        "AverageDistanceToMainPath",
    ] + BEHAVIOR_COLUMNS

    missing = [col for col in required_columns if col not in df.columns]
    if missing:
        raise ValueError(f"Missing required columns in CSV: {missing}")

    # --------------------------------------------------------
    # Step 1: filter out rows where FILTER_COLUMN == FILTER_VALUE
    # --------------------------------------------------------
    df = df[~df[FILTER_COLUMN].isin(FILTER_VALUES)].copy()

    # --------------------------------------------------------
    # Step 2: optional include filter
    # --------------------------------------------------------
    if INCLUDE_PLAYER_IDS:
        df = df[df[PLAYER_ID_COLUMN].astype(str).isin(set(INCLUDE_PLAYER_IDS))].copy()

    if df.empty:
        return []

    # --------------------------------------------------------
# Step 3: feature engineering (NEW)
# --------------------------------------------------------

    feature_df = pd.DataFrame(index=df.index)

    def get_level_const_from_row(row, key):
        behavior_key = get_behavior_key(row)

        if behavior_key not in LEVEL_CONSTANTS_BY_BEHAVIOR:
            return np.nan

        return LEVEL_CONSTANTS_BY_BEHAVIOR[behavior_key][key]


    # --- Percent features ---

    feature_df["PowerUpsTakenPercent"] = [
        safe_div(row["PowerUpsTaken"], get_level_const_from_row(row, "powerups"))
        for _, row in df.iterrows()
    ]

    feature_df["HealthBarrelsTakenPercent"] = [
        safe_div(row["HealthBarrelsTaken"], get_level_const_from_row(row, "health"))
        for _, row in df.iterrows()
    ]

    feature_df["AverageEnemiesAliveOnPOTakenPct"] = [
        safe_div(row["AvgEnemiesAliveOnPowerUpTaken"], get_level_const_from_row(row, "enemies"))
        for _, row in df.iterrows()
    ]


    # --- Direct features from CSV ---

    feature_df["OptionalRoomPercentage"] = pd.to_numeric(
        df["OptionalRoomPercentage"], errors="coerce"
    ).fillna(0.0)

    feature_df["AverageDistanceToMainPath"] = pd.to_numeric(
        df["AverageDistanceToMainPath"], errors="coerce"
    ).fillna(0.0)


    # Clean
    feature_df = feature_df.replace([np.inf, -np.inf], np.nan).fillna(0.0)

    # --------------------------------------------------------
    # Step 4: normalize
    # --------------------------------------------------------
    norm_df = normalize_features(feature_df, NORMALIZATION_METHOD)

    # --------------------------------------------------------
    # Step 5: build result
    # --------------------------------------------------------
    results = []

    for idx in df.index:
        row = df.loc[idx]

        features = [float(norm_df.loc[idx, col]) for col in norm_df.columns]

        behavior = [
            row[col] if not pd.isna(row[col]) else None
            for col in BEHAVIOR_COLUMNS
        ]

        results.append({
            "features": features,
            "feature_names": list(norm_df.columns),
            "info": {
                "playerId": row[PLAYER_ID_COLUMN],
                "levelPlayID": row[LEVEL_PLAY_ID_COLUMN],
                "behavior5": behavior,
            }
        })

    return results