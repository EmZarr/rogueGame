import numpy as np
import pandas as pd


# ============================================================
# CENTRAL CONFIG
# Change values here only.
# ============================================================

CSV_PATH = "Experiment3RandomMapsData.csv"

FILTER_COLUMN = "GeometryBehavior"
FILTER_VALUES = [0]

INCLUDE_PLAYER_IDS = []#["4036b402136b005f97a637e85f7961f4a1df5e23", "a337940f472f2f83c15e80db566145fe9e844d0d", "bbd174147e56b9d0ecff534e0bc4e7aa42920e66", "61ba1ad1e381c2a7ccb892a864aad6a50daf8d96", "86c8cfe04bc19458adcf3c880baccfeb7a03bd7c", "5953ebc01c53dc47066142b416c932f7e8a07982", "e967dc9b7177e4abf497f68b06561fcdd3fb0ebd", "c905b72819f4bbd69818665d6c720bddc85ce1a2"]

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
    (5, 1, 2, 78, 1): {"powerups": 7,  "health": 9,  "enemies": 26},
    (3, 2, 2, 50, 0): {"powerups": 9,  "health": 10,  "enemies": 22},
    (1, 2, 1, 103, 1): {"powerups": 11,  "health": 7,  "enemies": 33},
    (7, 1, 0, 104, 1): {"powerups": 36,  "health": 34,  "enemies": 130},
    (4, 1, 0, 1, 1): {"powerups": 12,  "health": 7,  "enemies": 14},
    (3, 2, 2, 56, 1): {"powerups": 9,  "health": 10,  "enemies": 12},
}

#Experiment 1 levels
"""
    # Level 1
    (1, 4, 1, 63, 1): {"powerups": 18,  "health": 12,  "enemies": 5},

    # Level 2
    (10, 4, 2, 50, 1): {"powerups": 16,  "health": 14,  "enemies": 39},

    # Level 3
    (4, 2, 0, 44, 1): {"powerups": 24, "health": 6,  "enemies": 22},

    # Level 4
    (19, 3, 2, 51, 2): {"powerups": 12,  "health": 18, "enemies": 35},

    # Level 5
    (4, 3, 0, 38, 2): {"powerups": 25, "health": 5,  "enemies": 24},

    (69, 2, 1, 41, 2): {"powerups": 18, "health": 12,  "enemies": 169},
    
    (12, 3, 2, 31, 2): {"powerups": 15, "health": 15,  "enemies": 47},
    
    (31, 2, 4, 41, 2): {"powerups": 5, "health": 25,  "enemies": 58},
    
    (3, 3, 1, 5, 2): {"powerups": 21, "health": 9,  "enemies": 12},
    
    (57, 2, 2, 43, 3): {"powerups": 16, "health": 14,  "enemies": 102},
    
    (4, 2, 3, 84, 3): {"powerups": 9, "health": 21,  "enemies": 41},
    
    (7, 3, 2, 41, 3): {"powerups": 17, "health": 13,  "enemies": 48},
    
    (69, 1, 2, 41, 3): {"powerups": 14, "health": 16,  "enemies": 271},
    
    (38, 2, 0, 28, 3): {"powerups": 24, "health": 6,  "enemies": 66},
    
    (16, 1, 1, 31, 3): {"powerups": 18, "health": 12,  "enemies": 73},
    
    (44, 2, 2, 50, 3): {"powerups": 15, "health": 15,  "enemies": 95},
    
    (18, 3, 4, 41, 3): {"powerups": 5, "health": 25,  "enemies": 121},

"""

#Experiment 2 Levels:
"""
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
"""

#Experiment 3-A levels:
"""
    (4, 0, 1, 20, 0): {"powerups": 6,  "health": 5,  "enemies": 42},
    (0, 2, 2, 0, 2): {"powerups": 13,  "health": 15,  "enemies": 27},
    (9, 2, 2, 123, 0): {"powerups": 11,  "health": 18,  "enemies": 36},
    (0, 2, 0, 103, 1): {"powerups": 11,  "health": 17,  "enemies": 43},
    (7, 2, 0, 55, 0): {"powerups": 10,  "health": 16,  "enemies": 26},
    (7, 0, 2, 47, 1): {"powerups": 6,  "health": 6,  "enemies": 32},
"""
#Experiment 3-B levels:
"""
    (5, 1, 2, 78, 1): {"powerups": 7,  "health": 9,  "enemies": 26},
    (3, 2, 2, 50, 0): {"powerups": 9,  "health": 10,  "enemies": 22},
    (1, 2, 1, 103, 1): {"powerups": 11,  "health": 7,  "enemies": 33},
    (7, 1, 0, 104, 1): {"powerups": 36,  "health": 34,  "enemies": 130},
    (4, 1, 0, 1, 1): {"powerups": 12,  "health": 7,  "enemies": 14},
    (3, 2, 2, 56, 1): {"powerups": 9,  "health": 10,  "enemies": 12},
"""
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