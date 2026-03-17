import numpy as np
from sklearn.cluster import KMeans, DBSCAN, AgglomerativeClustering
from sklearn.decomposition import PCA

from FilteredFeatures import process_csv


# ============================================================
# CLUSTER CONFIG
# Change clustering settings here only.
# ============================================================

CLUSTERING_ALGORITHM = "kmeans"
CLUSTERING_KWARGS = {
    "n_clusters": 3,
    "random_state": 42,
    "n_init": 10,
}

PROJECTION_METHOD = "pca"
PROJECTION_KWARGS = {
    "random_state": 42,
}


def make_clusterer(name="kmeans", **kwargs):
    name = name.lower()

    if name == "kmeans":
        return KMeans(
            n_clusters=kwargs.get("n_clusters", 3),
            random_state=kwargs.get("random_state", 42),
            n_init=kwargs.get("n_init", 10),
        )

    if name == "dbscan":
        return DBSCAN(
            eps=kwargs.get("eps", 0.5),
            min_samples=kwargs.get("min_samples", 5),
        )

    if name == "agglomerative":
        return AgglomerativeClustering(
            n_clusters=kwargs.get("n_clusters", 3),
            linkage=kwargs.get("linkage", "ward"),
        )

    raise ValueError(f"Unsupported clustering algorithm: {name}")


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


def print_cluster_report(labels, X, feature_names):
    print("\n==============================")
    print("CLUSTER PROFILES")
    print("==============================")

    unique_labels = sorted(set(labels))
    global_mean = X.mean(axis=0)

    for label in unique_labels:
        mask = labels == label
        count = int(mask.sum())

        print(f"\nCluster {label} ({count} rows)")

        if count == 0:
            continue

        cluster_mean = X[mask].mean(axis=0)
        diffs = cluster_mean - global_mean

        pairs = list(zip(feature_names, cluster_mean, diffs))
        pairs.sort(key=lambda x: abs(x[2]), reverse=True)

        for feature_name, mean_val, diff in pairs[:6]:
            direction = "higher" if diff > 0 else "lower"
            print(f"  {feature_name}: {mean_val:.4f} ({direction} than average by {diff:.4f})")


def cluster_entries(entries=None, return_projection_model=False):
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

    clusterer = make_clusterer(CLUSTERING_ALGORITHM, **CLUSTERING_KWARGS)
    labels = clusterer.fit_predict(X_model)

    projector = make_projector(PROJECTION_METHOD, n_components=2, **PROJECTION_KWARGS)
    coords_2d = projector.fit_transform(X_model)

    print(f"\nClustering algorithm: {CLUSTERING_ALGORITHM}")
    print(f"Projection method: {PROJECTION_METHOD}")
    print(f"Rows: {len(entries)}")
    print(f"Features: {len(feature_names)}")

    print_projection_report(projector, feature_names)
    print_cluster_report(labels, X_model, feature_names)

    center_coords_2d = None
    center_feature_space = None

    if hasattr(clusterer, "cluster_centers_"):
        center_feature_space = clusterer.cluster_centers_
        center_coords_2d = projector.transform(center_feature_space)

        print("\n==============================")
        print("CLUSTER CENTERS")
        print("==============================")
        for cluster_id, center in enumerate(center_feature_space):
            print(f"\nCluster {cluster_id}")
            pairs = list(zip(feature_names, center))
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

        label = int(labels[i])
        new_entry["info"]["cluster_label"] = label
        new_entry["info"]["plot_coordinates"] = coords_2d[i].tolist()
        new_entry["info"]["plot_method"] = PROJECTION_METHOD

        if center_coords_2d is not None:
            new_entry["info"]["cluster_center_coordinates"] = center_coords_2d[label].tolist()
            new_entry["info"]["cluster_center_feature_space"] = center_feature_space[label].tolist()

        result.append(new_entry)

    if return_projection_model:
        return result, projector

    return result