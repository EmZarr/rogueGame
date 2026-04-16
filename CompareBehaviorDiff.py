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

DISTANCE_METRIC = "l1"

MIN_SAME_BEHAVIOR_PAIRS = 5
MIN_DIFF_BEHAVIOR_PAIRS = 5

ONE_SIDED_HYPOTHESIS = True

PRINT_PLAYER_DETAILS = True

# Optional metric names
METRIC_NAMES = None


# ============================================================
# HELPERS
# ============================================================

def canonicalize_behavior(behavior):
    if behavior is None:
        return None

    out = []
    for x in behavior:
        if x is None:
            out.append(None)
        elif isinstance(x, (int, np.integer)):
            out.append(int(x))
        elif isinstance(x, (float, np.floating)):
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
    differences = np.asarray(differences, dtype=float)
    nonzero = differences[differences != 0]

    if len(nonzero) == 0:
        return 0.0

    pos = np.sum(nonzero > 0)
    neg = np.sum(nonzero < 0)
    return (pos - neg) / len(nonzero)


# ============================================================
# NEW: METRIC-LEVEL ANALYSIS
# ============================================================

def compute_metric_differences(runs):
    same_diffs = []
    diff_diffs = []

    for run_a, run_b in itertools.combinations(runs, 2):
        vec_a = run_a["weights"]
        vec_b = run_b["weights"]

        diff_vec = vec_b - vec_a

        if run_a["behavior"] == run_b["behavior"]:
            same_diffs.append(diff_vec)
        else:
            diff_diffs.append(diff_vec)

    return {"same": same_diffs, "diff": diff_diffs}


def summarize_metric_block(diff_list):
    if not diff_list:
        return None

    arr = np.vstack(diff_list)

    return {
        "mean_abs": np.mean(np.abs(arr), axis=0),
        "mean_signed": np.mean(arr, axis=0),
        "std": np.std(arr, axis=0),
    }


def print_metric_effects(player_results, metric_names=None):
    print("\n==============================")
    print("METRIC-LEVEL EFFECTS PER PLAYER")
    print("==============================")

    for r in player_results:
        same_stats = r.get("same_metric_stats")
        diff_stats = r.get("diff_metric_stats")

        if same_stats is None or diff_stats is None:
            continue

        print(f"\nPlayer {r['playerId']}")

        n_metrics = len(same_stats["mean_abs"])

        for i in range(n_metrics):
            name = metric_names[i] if metric_names else f"metric_{i}"

            same_val = same_stats["mean_abs"][i]
            diff_val = diff_stats["mean_abs"][i]
            delta = diff_val - same_val
            direction = diff_stats["mean_signed"][i]

            print(f"  {name}:")
            print(f"    same-behavior change:      {same_val:.6f}")
            print(f"    different-behavior change: {diff_val:.6f}")
            print(f"    delta (diff - same):       {delta:.6f}")
            print(f"    direction (signed mean):   {direction:.6f}")


# ============================================================
# CORE
# ============================================================
def build_level_transitions(runs):
    """
    Builds directed transitions between behaviors (levels).

    IMPORTANT:
    We treat each run as independent snapshots.
    If ordering exists, we could improve this further,
    but this version uses all ordered pairs consistently.
    """

    transitions = defaultdict(list)

    for run_a, run_b in itertools.combinations(runs, 2):

        a_level = run_a["behavior"]
        b_level = run_b["behavior"]

        if a_level == b_level:
            continue

        vec_a = run_a["weights"]
        vec_b = run_b["weights"]

        delta = vec_b - vec_a

        transitions[(a_level, b_level)].append(delta)

    return transitions

def summarize_transitions(transitions):
    """
    Converts raw transition deltas into per-metric averages.
    """

    summary = {}

    for (a, b), deltas in transitions.items():
        arr = np.vstack(deltas)

        summary[(a, b)] = {
            "mean": np.mean(arr, axis=0),
            "abs_mean": np.mean(np.abs(arr), axis=0),
            "count": len(deltas)
        }

    return summary


def print_level_transition_effects(player_results, metric_names=None, top_k=None):
    print("\n==============================")
    print("LEVEL-TO-LEVEL METRIC TRANSITIONS (PER PLAYER)")
    print("==============================")

    for r in player_results:
        print(f"\nPlayer {r['playerId']}")

        transitions = r.get("level_transition_summary")
        if not transitions:
            continue

        # sort by strongest overall effect
        sorted_items = sorted(
            transitions.items(),
            key=lambda x: np.linalg.norm(x[1]["mean"]),
            reverse=True
        )

        if top_k:
            sorted_items = sorted_items[:top_k]

        for (a, b), stats in sorted_items:

            print(f"\n  {a}  →  {b}   (n={stats['count']})")

            for i, val in enumerate(stats["mean"]):
                name = metric_names[i] if metric_names else f"metric_{i}"

                direction = "↑" if val > 0 else "↓" if val < 0 else "→"

                print(f"    {name}: {val:.6f} {direction}")


def build_player_behavior_pairs(entries):
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

        # NEW metric-level analysis
        metric_data = compute_metric_differences(runs)
        same_metric_stats = summarize_metric_block(metric_data["same"])
        diff_metric_stats = summarize_metric_block(metric_data["diff"])

        level_transitions = build_level_transitions(runs)
        level_summary = summarize_transitions(level_transitions)

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

            # NEW
            "same_metric_stats": same_metric_stats,
            "diff_metric_stats": diff_metric_stats,
            "level_transition_summary": level_summary,
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


def build_global_level_transitions(all_player_results):
    """
    Aggregates level transitions across ALL players.
    """

    global_transitions = defaultdict(list)

    for r in all_player_results:
        transitions = r.get("level_transition_summary")
        if not transitions:
            continue

        for (a, b), stats in transitions.items():
            # stats["mean"] is per-player mean delta vector
            global_transitions[(a, b)].append(stats["mean"])

    return global_transitions

def summarize_global_transitions(global_transitions):
    summary = {}

    for (a, b), vecs in global_transitions.items():
        arr = np.vstack(vecs)

        summary[(a, b)] = {
            "mean": np.mean(arr, axis=0),
            "std": np.std(arr, axis=0),
            "n_players": len(vecs)
        }

    return summary

def print_global_transition_effects(global_summary, metric_names=None, top_k=10):
    print("\n==============================")
    print("GLOBAL LEVEL-TO-LEVEL METRIC EFFECTS (ACROSS PLAYERS)")
    print("==============================")

    sorted_items = sorted(
        global_summary.items(),
        key=lambda x: np.linalg.norm(x[1]["mean"]),
        reverse=True
    )

    for (a, b), stats in sorted_items[:top_k]:

        print(f"\n{a}  →  {b}   (players={stats['n_players']})")

        for i, val in enumerate(stats["mean"]):
            name = metric_names[i] if metric_names else f"metric_{i}"
            direction = "↑" if val > 0 else "↓" if val < 0 else "→"

            print(f"  {name}: {val:.6f} {direction}")

def interpret_metrics(entries):
    """
    Correlate archetype metrics with raw features to understand meaning.
    """

    # Collect data
    weights = []
    features = defaultdict(list)

    for entry in entries:
        info = entry["info"]

        w = np.asarray(info["archetype_weights"], dtype=float)
        weights.append(w)

        # 👇 CHANGE THIS: add your raw features here
        for key, val in info.items():
            if isinstance(val, (int, float)):
                features[key].append(val)

    weights = np.vstack(weights)

    print("\n==============================")
    print("METRIC INTERPRETATION (CORRELATIONS)")
    print("==============================")

    for i in range(weights.shape[1]):
        print(f"\nmetric_{i}:")

        correlations = []

        for key, vals in features.items():
            if len(vals) != len(weights):
                continue

            vals = np.asarray(vals)

            if np.std(vals) == 0:
                continue

            corr = np.corrcoef(weights[:, i], vals)[0, 1]

            if not np.isnan(corr):
                correlations.append((key, corr))

        # sort strongest relationships
        correlations.sort(key=lambda x: abs(x[1]), reverse=True)

        for key, corr in correlations[:5]:
            print(f"  {key}: {corr:.3f}")            

def compare_same_vs_different_behavior(entries=None):
    if entries is None:
        entries = analyze_entries()

    if not entries:
        print("No entries found.")
        return None

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
        if (r["n_same_behavior_pairs"] >= MIN_SAME_BEHAVIOR_PAIRS and
            r["n_diff_behavior_pairs"] >= MIN_DIFF_BEHAVIOR_PAIRS):
            included_players.append(r)
        else:
            excluded_players.append(r)

    if PRINT_PLAYER_DETAILS:
        print_player_report(player_results)

    print_metric_effects(player_results, METRIC_NAMES)

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
        print("\nNo valid players for statistical test.")
        return None

    same_means = np.array([r["same_behavior_mean"] for r in included_players])
    diff_means = np.array([r["diff_behavior_mean"] for r in included_players])
    paired_diffs = diff_means - same_means
    interpret_metrics(entries)
    print("\n==============================")
    print("PLAYER-LEVEL AVERAGE DISTANCES")
    print("==============================")
    summarize("Mean distance within same behavior", same_means)
    summarize("Mean distance across different behaviors", diff_means)
    summarize("Difference (different - same)", paired_diffs)

    print_level_transition_effects(player_results, METRIC_NAMES, top_k=10)

    global_transitions = build_global_level_transitions(player_results)
    global_summary = summarize_global_transitions(global_transitions)

    print_global_transition_effects(global_summary, METRIC_NAMES, top_k=15)

    

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