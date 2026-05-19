import numpy as np
import pandas as pd
import matplotlib.pyplot as plt

from archetypes import AA

# Import your existing preprocessing pipeline
from FilteredFeatures import process_csv


# ============================================================
# SETTINGS
# ============================================================

CSV_FILE = "your_dataset.csv"

MIN_K = 1
MAX_K = 20

# ============================================================
# LOAD + PROCESS FEATURES
# ============================================================

# Your function returns a list of dictionaries
results = process_csv()

# Extract feature vectors
X = np.array([
    row["features"]
    for row in results
], dtype=float)

# Optional: remove rows with NaNs just in case
X = X[~np.isnan(X).any(axis=1)]

print("Feature matrix shape:", X.shape)

# ============================================================
# RUN ARCHETYPAL ANALYSIS
# ============================================================

rss_values = []
k_values = list(range(MIN_K, MAX_K + 1))

for k in k_values:

    print(f"\nRunning archetypal analysis with k={k}")

    model = AA(
        n_archetypes=k,
        max_iter=500,
        tol=1e-4,
        random_state=42
    )

    # Fit model
    model.fit(X)

    # --------------------------------------------------------
    # Reconstruct dataset
    # --------------------------------------------------------

    # Alpha matrix:
    # shape = (samples x archetypes)
    alpha = model.transform(X)

    # Archetypes:
    # shape = (archetypes x features)
    archetypes = model.archetypes_

    # Reconstruction
    X_hat = alpha @ archetypes

    # --------------------------------------------------------
    # Compute RSS
    # --------------------------------------------------------

    rss = np.sum((X - X_hat) ** 2)

    rss_values.append(rss)

    print(f"RSS = {rss:.4f}")

# ============================================================
# PRINT RESULTS
# ============================================================

print("\n==============================")
print("RSS RESULTS")
print("==============================")

for k, rss in zip(k_values, rss_values):
    print(f"k = {k:2d}   RSS = {rss:.4f}")

# ============================================================
# SCREE PLOT
# ============================================================

plt.figure(figsize=(8, 5))

plt.plot(
    k_values,
    rss_values,
    marker='o',
    linewidth=2
)

plt.xlabel("Number of Archetypes (k)")
plt.ylabel("Residual Sum of Squares (RSS)")
plt.title("Archetypal Analysis Scree Plot Exploration")

plt.xticks(k_values)

plt.grid(True)

plt.tight_layout()

plt.show()