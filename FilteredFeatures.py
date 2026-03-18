import numpy as np
import pandas as pd


# ============================================================
# CENTRAL CONFIG
# Change values here only.
# ============================================================

CSV_PATH = "Telemetry_Raw.csv"

FILTER_COLUMN = "GeometryBehavior"
FILTER_VALUES = [1]

INCLUDE_PLAYER_IDS = []

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


# ============================================================
# HELPERS
# ============================================================

def safe_div(numerator, denominator, default=0.0):
    if pd.isna(denominator) or denominator == 0:
        return default
    if pd.isna(numerator):
        return default
    return float(numerator) / float(denominator)


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
        "timePlayed",
        "AverageDistanceToEnemies",
        "bowDefense",
        "knightDefense",
        "berserkDefense",
        "bowLightAtk",
        "knightLightAtk",
        "berserkLightAtk",
        "bowHeavyAtk",
        "knightHeavyAtk",
        "berserkHeavyAtk",
        "damageTakenMelee",
        "damageTakenRanged",
        "damageTakenGuardianShield",
        "damageTakenTraps",
        "bowLightDash",
        "knightLightDash",
        "berserkLightDash",
        "bowHeavyDash",
        "knightHeavyDash",
        "berserkHeavyDash",
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
    # Step 3: feature engineering
    # --------------------------------------------------------
    feature_df = pd.DataFrame(index=df.index)

    total_defense = (
        df["bowDefense"] +
        df["knightDefense"] +
        df["berserkDefense"]
    )

    total_light_attacks = (
        df["bowLightAtk"] +
        df["knightLightAtk"] +
        df["berserkLightAtk"]
    )

    total_heavy_attacks = (
        df["bowHeavyAtk"] +
        df["knightHeavyAtk"] +
        df["berserkHeavyAtk"]
    )

    total_attacks = total_light_attacks + total_heavy_attacks

    total_damage_taken = (
        df["damageTakenMelee"] +
        df["damageTakenRanged"] +
        df["damageTakenGuardianShield"] +
        df["damageTakenTraps"]
    )

    total_light_dashes = (
        df["bowLightDash"] +
        df["knightLightDash"] +
        df["berserkLightDash"]
    )

    total_heavy_dashes = (
        df["bowHeavyDash"] +
        df["knightHeavyDash"] +
        df["berserkHeavyDash"]
    )



    feature_df["avg_distance_to_enemies"] = df["AverageDistanceToEnemies"]

    feature_df["defense_per_time"] = [
        safe_div(x, t) for x, t in zip(total_defense, df["timePlayed"])
    ]

    feature_df["attacks_per_time"] = [
        safe_div(x, t) for x, t in zip(total_attacks, df["timePlayed"])
    ]

    feature_df["heavy_to_light_ratio"] = [
        safe_div(h, l) for h, l in zip(total_heavy_attacks, total_light_attacks)
    ]

    feature_df["damage_taken_per_time"] = [
        safe_div(x, t) for x, t in zip(total_damage_taken, df["timePlayed"])
    ]

    feature_df["light_dashes_per_time"] = [
        safe_div(x, t) for x, t in zip(total_light_dashes, df["timePlayed"])
    ]

    feature_df["heavy_dashes_per_time"] = [
        safe_div(x, t) for x, t in zip(total_heavy_dashes, df["timePlayed"])
    ]

    feature_df["RangedAttackShare"] = [
        safe_div(x, t) for x, t in zip(df["bowLightAtk"], total_light_attacks)
    ]

    feature_df["RangedDashShare"] = [
        safe_div(x, t) for x, t in zip(df["bowLightDash"], total_light_dashes)
    ]

    feature_df["RogueDashShare"] = [
        safe_div(x, t) for x, t in zip(df["berserkLightDash"], total_light_dashes)
    ]

    feature_df["KnightDashShare"] = [
        safe_div(x, t) for x, t in zip(df["knightLightDash"], total_light_dashes)
    ]

    feature_df["RangedHeavyDashShare"] = [
        safe_div(x, t) for x, t in zip(df["bowHeavyDash"], total_heavy_dashes)
    ]

    feature_df["RogueHeavyDashShare"] = [
        safe_div(x, t) for x, t in zip(df["berserkHeavyDash"], total_heavy_dashes)
    ]

    feature_df["KnightHeavyDashShare"] = [
        safe_div(x, t) for x, t in zip(df["knightHeavyDash"], total_heavy_dashes)
    ]

    feature_df["RangedDefShare"] = [
        safe_div(x, t) for x, t in zip(df["bowDefense"], total_defense)
    ]

    feature_df["RogueDefShare"] = [
        safe_div(x, t) for x, t in zip(df["berserkDefense"], total_defense)
    ]

    feature_df["KnightDefShare"] = [
        safe_div(x, t) for x, t in zip(df["knightDefense"], total_defense)
    ]

    feature_df["FormChanges"] = [
        safe_div(x, t) for x, t in zip(df["FormChangeCountInCombat"], df["timePlayed"])
    ]

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