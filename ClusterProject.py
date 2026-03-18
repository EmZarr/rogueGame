import numpy as np
from sklearn.decomposition import PCA

from FilteredFeatures import process_csv

# Compatibility patch for py_pcha on NumPy 2.x
if not hasattr(np, "mat"):
    np.mat = np.asmatrix

# Archetypal Analysis backend
# pip install py-pcha
from py_pcha import PCHA


# ============================================================
# MODEL CONFIG
# Change analysis settings here only.
# ============================================================

ANALYSIS_ALGORITHM = "archetypal"

ANALYSIS_KWARGS = {
    "n_archetypes": 3,   # number of archetypes
    "delta": 0.1,        # regularization / robustness parameter for PCHA
}

PROJECTION_METHOD = "pca"
PROJECTION_KWARGS = {
    "random_state": 42,
}


class ArchetypalAnalysisModel:
    """
    Thin wrapper around py_pcha so the rest of the code has a
    scikit-learn-like interface.
    """

    def __init__(self, n_archetypes=3, delta=0.1):
        self.n_archetypes = n_archetypes
        self.delta = delta

        self.archetypes_ = None              # shape: (k, n_features)
        self.membership_weights_ = None      # shape: (n_samples, k)
        self.labels_ = None                  # hard assignment via argmax
        self.sse_ = None
        self.variance_explained_ = None

    def fit(self, X):
        """
        py_pcha expects shape (dimensions, examples),
        while sklearn usually uses (examples, dimensions).
        So we transpose before/after.
        """
        X_t = X.T  # shape: (n_features, n_samples)

        XC, S, C, SSE, varexpl = PCHA(
            X_t,
            noc=self.n_archetypes,
            delta=self.delta,
        )

        # XC: archetypes in feature space, shape (n_features, k)
        # S: coefficients to reconstruct each sample from archetypes
        #    shape usually (k, n_samples)
        self.archetypes_ = np.asarray(XC.T, dtype=float)
        self.membership_weights_ = np.asarray(S.T, dtype=float)
        self.labels_ = np.argmax(self.membership_weights_, axis=1).astype(int)
        self.sse_ = float(SSE)
        self.variance_explained_ = float(varexpl)

        return self

    def fit_predict(self, X):
        self.fit(X)
        return self.labels_


def make_analyzer(name="archetypal", **kwargs):
    name = name.lower()

    if name == "archetypal":
        return ArchetypalAnalysisModel(
            n_archetypes=kwargs.get("n_archetypes", 3),
            delta=kwargs.get("delta", 0.1),
        )

    raise ValueError(f"Unsupported analysis algorithm: {name}")


def make_projector(name="pca", n_components=2, **kwargs):
    name = name.lower()

    if name == "pca":
        return PCA(
            n_components=n_components,
            random_state=kwargs.get("random_state", 42),
        )

    raise ValueError(f"Unsupported projection method: {name}")


def print_projection_report(projector, feature_names):
    if not hasattr(projector, "components_"):
        return

    print("\n==============================")
    print("PROJECTION INTERPRETATION")
    print("==============================")

    if hasattr(projector, "explained_variance_ratio_"):
        for i, var in enumerate(projector.explained_variance_ratio_):
            print(f"PC{i+1}: {var:.4f} ({var*100:.2f}%)")

    for axis_idx, component in enumerate(projector.components_):
        print(f"\nTop contributing features for axis {axis_idx + 1}:")
        pairs = list(zip(feature_names, component))
        pairs.sort(key=lambda x: abs(x[1]), reverse=True)

        for feature_name, weight in pairs[:8]:
            print(f"  {feature_name}: {weight:.4f}")


def print_archetype_report(archetypes, X, feature_names):
    print("\n==============================")
    print("ARCHETYPE PROFILES")
    print("==============================")

    global_mean = X.mean(axis=0)

    for idx, archetype in enumerate(archetypes):
        print(f"\nArchetype {idx}")

        diffs = archetype - global_mean
        pairs = list(zip(feature_names, archetype, diffs))
        pairs.sort(key=lambda x: abs(x[2]), reverse=True)

        for feature_name, value, diff in pairs[:6]:
            direction = "higher" if diff > 0 else "lower"
            print(f"  {feature_name}: {value:.4f} ({direction} than average by {diff:.4f})")


def print_membership_report(weights):
    print("\n==============================")
    print("ARCHETYPE MIXTURE SUMMARY")
    print("==============================")

    mean_weights = weights.mean(axis=0)
    for i, w in enumerate(mean_weights):
        print(f"Archetype {i}: mean membership {w:.4f}")


def analyze_entries(entries=None, return_projection_model=False):
    """
    If entries is None, it automatically calls process_csv().
    """

    if entries is None:
        entries = process_csv()

    if not entries:
        if return_projection_model:
            return [], None
        return []

    feature_names = entries[0]["feature_names"]
    X = np.array([entry["features"] for entry in entries], dtype=float)

    # Already normalized in FilteredFeatures.py
    X_model = X

    analyzer = make_analyzer(ANALYSIS_ALGORITHM, **ANALYSIS_KWARGS)
    labels = analyzer.fit_predict(X_model)

    projector = make_projector(PROJECTION_METHOD, n_components=2, **PROJECTION_KWARGS)
    coords_2d = projector.fit_transform(X_model)

    print(f"\nAnalysis algorithm: {ANALYSIS_ALGORITHM}")
    print(f"Projection method: {PROJECTION_METHOD}")
    print(f"Rows: {len(entries)}")
    print(f"Features: {len(feature_names)}")

    if hasattr(analyzer, "variance_explained_"):
        print(f"Variance explained by archetypal model: {analyzer.variance_explained_:.4f}")
    if hasattr(analyzer, "sse_"):
        print(f"Reconstruction SSE: {analyzer.sse_:.4f}")

    print_projection_report(projector, feature_names)
    print_archetype_report(analyzer.archetypes_, X_model, feature_names)
    print_membership_report(analyzer.membership_weights_)

    archetype_coords_2d = projector.transform(analyzer.archetypes_)

    print("\n==============================")
    print("ARCHETYPES")
    print("==============================")
    for archetype_id, archetype in enumerate(analyzer.archetypes_):
        print(f"\nArchetype {archetype_id}")
        pairs = list(zip(feature_names, archetype))
        pairs.sort(key=lambda x: abs(x[1]), reverse=True)
        for feature_name, value in pairs[:8]:
            print(f"  {feature_name}: {value:.4f}")

    result = []

    for i, entry in enumerate(entries):
        new_entry = {
            "features": entry["features"],
            "feature_names": entry["feature_names"],
            "info": dict(entry["info"]),
        }

        hard_label = int(labels[i])
        weights = analyzer.membership_weights_[i]

        new_entry["info"]["cluster_label"] = hard_label
        new_entry["info"]["archetype_label"] = hard_label
        new_entry["info"]["archetype_weights"] = weights.tolist()
        new_entry["info"]["dominant_archetype"] = hard_label
        new_entry["info"]["plot_coordinates"] = coords_2d[i].tolist()
        new_entry["info"]["plot_method"] = PROJECTION_METHOD
        new_entry["info"]["archetype_coordinates"] = archetype_coords_2d.tolist()
        new_entry["info"]["archetypes_feature_space"] = analyzer.archetypes_.tolist()

        result.append(new_entry)

    if return_projection_model:
        return result, projector

    return result


# Backward-compatible alias if other code still calls cluster_entries(...)
def cluster_entries(entries=None, return_projection_model=False):
    return analyze_entries(entries=entries, return_projection_model=return_projection_model)