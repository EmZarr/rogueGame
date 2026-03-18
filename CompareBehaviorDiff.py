import itertools
from collections import defaultdict

import numpy as np
from scipy.stats import wilcoxon

# Change this import to your actual filename
from ClusterProject import analyze_entries


# ============================================================
# CONFIG
# ============================================================

PLAYER_KEY = "playerId"
BEHAVIOR_KEY = "behavior5"

# Distance between archetype-weight vectors
# Options: "l1", "l2", "cosine"
DISTANCE_METRIC = "l1"

# Minimum number of pair-comparisons required per player
MIN_SAME_BEHAVIOR_PAIRS = 1
MIN_DIFF_BEHAVIOR_PAIRS = 1

# If True, hypothesis test becomes:
#   different-behavior average distance > same-behavior average distance
ONE_SIDED_HYPOTHESIS = True

PRINT_PLAYER_DETAILS = True


# ============================================================
# HELPERS
# ============================================================

def canonicalize_behavior(behavior):
    """
    Convert behavior5 list into a hashable comparable tuple.
    """
    if behavior is None:
        return None

    out = []
    for x in behavior:
        if x is None:
            out.append(None)
        elif isinstance(x, (int, np.integer)):
            out.append(int(x))
        elif isinstance(x, (float, np.floating)):
            # avoid weird floating precision mismatches
            out.append(round(float(x), 10))
        else:
            out.append(x)
    return tuple(out)


def vector_distance(a, b, metric="l1"):
    a = np.asarray(a, dtype=float)
    b = np.asarray(b, dtype=float)

    if metric == "l1":
        return float(np.sum(np.abs(a - b)))
    elif metric == "l2":
        return float(np.linalg.norm(a - b))
    elif metric == "cosine":
        na = np.linalg.norm(a)
        nb = np.linalg.norm(b)
        if na == 0 or nb == 0:
            return 0.0
        cos_sim = np.dot(a, b) / (na * nb)
        cos_sim = np.clip(cos_sim, -1.0, 1.0)
        return float(1.0 - cos_sim)
    else:
        raise ValueError(f"Unsupported distance metric: {metric}")


def summarize(name, values):
    values = np.asarray(values, dtype=float)

    if len(values) == 0:
        print(f"{name}: no data")
        return

    print(f"{name}:")
    print(f"  n      = {len(values)}")
    print(f"  mean   = {np.mean(values):.6f}")
    print(f"  std    = {np.std(values, ddof=1) if len(values) > 1 else 0.0:.6f}")
    print(f"  median = {np.median(values):.6f}")
    print(f"  min    = {np.min(values):.6f}")
    print(f"  max    = {np.max(values):.6f}")


def paired_direction_effect(differences):
    """
    Simple directional effect summary.
    Positive means different-behavior > same-behavior.
    """
    differences = np.asarray(differences, dtype=float)
    nonzero = differences[differences != 0]

    if len(nonzero) == 0:
        return 0.0

    pos = np.sum(nonzero > 0)
    neg = np.sum(nonzero < 0)
    return (pos - neg) / len(nonzero)


# ============================================================
# CORE
# ============================================================

def build_player_behavior_pairs(entries):
    """
    For each player:
      - compare all pairs of that player's runs
      - split pairs into:
          same behavior5
          different behavior5
      - distance is measured on archetype_weights
    """
    by_player = defaultdict(list)

    for idx, entry in enumerate(entries):
        info = entry["info"]
        player_id = info[PLAYER_KEY]
        behavior = canonicalize_behavior(info[BEHAVIOR_KEY])
        weights = np.asarray(info["archetype_weights"], dtype=float)

        by_player[player_id].append({
            "entry_idx": idx,
            "behavior": behavior,
            "weights": weights,
        })

    player_results = []

    for player_id, runs in by_player.items():
        same_behavior_distances = []
        diff_behavior_distances = []

        behavior_counts = defaultdict(int)
        for run in runs:
            behavior_counts[run["behavior"]] += 1

        for run_a, run_b in itertools.combinations(runs, 2):
            dist = vector_distance(run_a["weights"], run_b["weights"], metric=DISTANCE_METRIC)

            if run_a["behavior"] == run_b["behavior"]:
                same_behavior_distances.append(dist)
            else:
                diff_behavior_distances.append(dist)

        player_results.append({
            "playerId": player_id,
            "n_runs": len(runs),
            "n_unique_behaviors": len(behavior_counts),
            "behavior_counts": dict(behavior_counts),
            "same_behavior_distances": same_behavior_distances,
            "diff_behavior_distances": diff_behavior_distances,
            "n_same_behavior_pairs": len(same_behavior_distances),
            "n_diff_behavior_pairs": len(diff_behavior_distances),
            "same_behavior_mean": float(np.mean(same_behavior_distances)) if same_behavior_distances else np.nan,
            "diff_behavior_mean": float(np.mean(diff_behavior_distances)) if diff_behavior_distances else np.nan,
        })

    return player_results


def print_player_report(player_results):
    print("\n==============================")
    print("PLAYER-LEVEL SUMMARY")
    print("==============================")

    for r in player_results:
        print(f"\nPlayer {r['playerId']}")
        print(f"  runs: {r['n_runs']}")
        print(f"  unique behaviors: {r['n_unique_behaviors']}")
        print(f"  same-behavior pairs: {r['n_same_behavior_pairs']}")
        print(f"  different-behavior pairs: {r['n_diff_behavior_pairs']}")

        print("  behavior counts:")
        for behavior, count in r["behavior_counts"].items():
            print(f"    {behavior}: {count}")

        if not np.isnan(r["same_behavior_mean"]):
            print(f"  mean same-behavior distance:      {r['same_behavior_mean']:.6f}")
        else:
            print("  mean same-behavior distance:      NA")

        if not np.isnan(r["diff_behavior_mean"]):
            print(f"  mean different-behavior distance: {r['diff_behavior_mean']:.6f}")
        else:
            print("  mean different-behavior distance: NA")


def compare_same_vs_different_behavior(entries=None):
    if entries is None:
        entries = analyze_entries()

    if not entries:
        print("No entries found.")
        return None

    for i, entry in enumerate(entries):
        if "info" not in entry:
            raise KeyError(f"Entry {i} has no 'info'.")
        if PLAYER_KEY not in entry["info"]:
            raise KeyError(f"Entry {i} missing info['{PLAYER_KEY}'].")
        if BEHAVIOR_KEY not in entry["info"]:
            raise KeyError(f"Entry {i} missing info['{BEHAVIOR_KEY}'].")
        if "archetype_weights" not in entry["info"]:
            raise KeyError(
                f"Entry {i} missing info['archetype_weights']. "
                "Make sure analyze_entries() was used."
            )

    print("==============================")
    print("SAME-BEHAVIOR VS DIFFERENT-BEHAVIOR TEST")
    print("==============================")
    print(f"Rows: {len(entries)}")
    print(f"Player key: {PLAYER_KEY}")
    print(f"Behavior key: {BEHAVIOR_KEY}")
    print(f"Distance metric: {DISTANCE_METRIC}")

    player_results = build_player_behavior_pairs(entries)

    included_players = []
    excluded_players = []

    for r in player_results:
        enough_same = r["n_same_behavior_pairs"] >= MIN_SAME_BEHAVIOR_PAIRS
        enough_diff = r["n_diff_behavior_pairs"] >= MIN_DIFF_BEHAVIOR_PAIRS

        if enough_same and enough_diff:
            included_players.append(r)
        else:
            excluded_players.append(r)

    if PRINT_PLAYER_DETAILS:
        print_player_report(player_results)

    print("\n==============================")
    print("INCLUSION SUMMARY")
    print("==============================")
    print(f"Players total:    {len(player_results)}")
    print(f"Players included: {len(included_players)}")
    print(f"Players excluded: {len(excluded_players)}")

    if excluded_players:
        print("\nExcluded players:")
        for r in excluded_players:
            print(
                f"  Player {r['playerId']} "
                f"(same-behavior pairs={r['n_same_behavior_pairs']}, "
                f"different-behavior pairs={r['n_diff_behavior_pairs']})"
            )

    if not included_players:
        print("\nNo players had both same-behavior and different-behavior comparisons.")
        return None

    same_means = np.array([r["same_behavior_mean"] for r in included_players], dtype=float)
    diff_means = np.array([r["diff_behavior_mean"] for r in included_players], dtype=float)
    paired_diffs = diff_means - same_means

    print("\n==============================")
    print("PLAYER-LEVEL AVERAGE DISTANCES")
    print("==============================")
    summarize("Mean distance within same behavior", same_means)
    summarize("Mean distance across different behaviors", diff_means)
    summarize("Difference (different - same)", paired_diffs)

    print("\n==============================")
    print("PRIMARY TEST: WILCOXON SIGNED-RANK")
    print("==============================")

    alternative = "greater" if ONE_SIDED_HYPOTHESIS else "two-sided"

    try:
        stat, p = wilcoxon(
            diff_means,
            same_means,
            zero_method="wilcox",
            alternative=alternative,
        )
        print(f"Alternative hypothesis: diff_mean > same_mean" if ONE_SIDED_HYPOTHESIS
              else "Alternative hypothesis: distributions differ")
        print(f"Wilcoxon statistic = {stat:.6f}")
        print(f"p-value            = {p:.6g}")
    except ValueError as e:
        stat, p = np.nan, np.nan
        print(f"Wilcoxon test could not be computed: {e}")

    effect_dir = paired_direction_effect(paired_diffs)
    print(f"Directional effect (different > same) = {effect_dir:.6f}")

    print("\n==============================")
    print("INTERPRETATION")
    print("==============================")

    mean_same = float(np.mean(same_means))
    mean_diff = float(np.mean(diff_means))
    median_same = float(np.median(same_means))
    median_diff = float(np.median(diff_means))

    print(f"Average player mean within same behavior:      {mean_same:.6f}")
    print(f"Average player mean across different behavior: {mean_diff:.6f}")
    print(f"Median player mean within same behavior:       {median_same:.6f}")
    print(f"Median player mean across different behavior:  {median_diff:.6f}")

    if mean_diff > mean_same:
        print("Observed direction: players differ MORE across different behaviors than within the same behavior.")
    elif mean_diff < mean_same:
        print("Observed direction: players differ MORE within the same behavior than across different behaviors.")
    else:
        print("Observed direction: no difference in means.")

    if not np.isnan(p):
        alpha = 0.05
        if p < alpha:
            print(f"Result: statistically significant at alpha = {alpha}.")
        else:
            print(f"Result: not statistically significant at alpha = {alpha}.")

    return {
        "player_results": player_results,
        "included_players": included_players,
        "same_means": same_means,
        "diff_means": diff_means,
        "paired_diffs": paired_diffs,
        "wilcoxon_stat": stat,
        "wilcoxon_p": p,
    }


if __name__ == "__main__":
    compare_same_vs_different_behavior()