# RogueGame — How to Run

This project has two halves:

- A **Unity side** that generates roguelike maps using MAP-Elites, CMA-ME, or random generation, then plays them.
- A **Python side** that analyzes player telemetry after testing sessions.

## Unity: generating, selecting, and playing maps

### The pipeline at a glance

The end-to-end flow always follows the same four steps:

1. **Generate** an archive of maps using one of the three generator scenes (CMA-ME, MAP-Elites, or Random).
2. **Move** the resulting JSON file from `Assets/` into `Assets/StreamingAssets/`.
3. **Select** a small diverse subset from that archive using the MapSelector scene.
4. **Play** the selected maps in the GamePlay scene.

> **Important:** the generator scenes save their output to the `Assets/` folder, but `MapSelector` and `GamePlay` read from `Assets/StreamingAssets/`. You have to manually move (or copy) the file between steps 1 and 3. The code does not do this for you.

### Generating an archive

You have three choices, depending on what kind of generator you want to use.

#### CMA-ME

**Scene:** `Assets/Scenes/CMA_ME.unity`

1. Open the scene.
2. Click the `grid` GameObject in the hierarchy.
3. In the inspector, find the **CMA_ME** component. You can tune:
   - **Total Iterations** - how many candidates each stage runs (enemy stage runs 3x this).
   - **Initial Random Solutions** - how many random seed candidates each stage starts with before mutation kicks in.
   - **Training Logger** - drag in the TrainingLogger GameObject if you want CSV progress logs.
4. Press **Play**.
5. When it finishes, you'll find three new files in your `Assets/` folder:
   - `geoArchiveEasy_maps.json`
   - `furnArchiveEasy_maps.json`
   - `enemArchiveEasy_maps.json` <- this is the one you'll use next

#### standard MAP-Elites

**Scene:** `Assets/Scenes/MapEliteTest.unity`

Same procedure as CMA-ME, but the output files are named without the "Easy" suffix:

- `geoArchive_maps.json`
- `furnArchive_maps.json`
- `enemArchive_maps.json` <- this one

#### random (no elite search)

**Scene:** `Assets/Scenes/RandomMapCreationScene.unity`

1. Open the scene.
2. Click the `grid` GameObject.
3. In the inspector, find the **RandomMapGeneratorEliteStyle** component. Tune:
   - **Random Map Amount** - how many random maps to generate per stage.
4. Press **Play**.
5. The output file is `Random_MapsUpdated.json` in `Assets/`.

### Moving the file (manual step)

After generation, find the enemy archive file in `Assets/` and **move it into `Assets/StreamingAssets/`**.
You can do this in the Unity Project window by dragging the file, or in your OS file explorer.
If `Assets/StreamingAssets/` doesn't exist yet, create it.

### Selecting a diverse subset

The full archive has hundreds of maps. The selector picks a small, maximally-diverse subset to play.
**Scene:** `Assets/Scenes/MapSelectorScene.unity`

1. Open the scene.
2. Click the `grid` GameObject.
3. In the inspector, find the **MapSelector** component. Set:
   - **Input File Name** - the filename you just moved into StreamingAssets (e.g. `enemArchiveEasy_maps.json`).
   - **Output File Name** - what to call the selected subset. **Default is `diverse_maps.json`** - the GamePlay scene expects exactly this name, so keep it unless you also change the LevelManager Script.
4. Press **Play**.
5. The output file is written into `Assets/StreamingAssets/` automatically (no manual move needed this time).

### Playing the maps

**Scene:** `Assets/Scenes/GamePlay.unity`

1. Make sure `diverse_maps.json` exists in `Assets/StreamingAssets/`.
2. Open the scene.
3. Press **Play**. The LevelManager loads 8 maps from the file and runs them as a level loop.

If you saved your selector output with a different name, edit `LevelManager.cs` line 71.

---

## Python: analyzing telemetry and rendering maps

### One-time setup

Install the packages used across all the scripts:

```bash
pip install numpy pandas scipy scikit-learn matplotlib seaborn statsmodels Pillow py-pcha archetypes
```

(Python 3.10+)
All Python scripts expect to be run from the **repository root**:

```bash
cd path/to/rogueGame
python ScriptName.py
```

### Rendering map previews (ArchiveRunner)

Turns a JSON archive into PNG thumbnails - one per unique behavior.
**Inputs:**

- A file named `maps.json` in the repository root. Rename any of your archive files (`enemArchiveEasy_maps.json`, `diverse_maps.json`, etc.) to `maps.json` before running.
  **Run:**

```bash
python ArchiveRunner.py
```

**Outputs:**

- An `icons/` folder full of PNGs, one per behavior, named `behavior_<>.png`.
- The hash matches the behavior tuple, so the same map always gets the same filename.

These PNGs are used by the visualizers below (DataVisualizer, ExplorationVisualizer) to show map previews when you click on data points.

### Analyzing telemetry

This is the workflow for going from a raw telemetry CSV to interactive plots.

**Inputs you need to provide:**

- `Telemetry_RawControl.csv` in the repository root - exported from your playtest sessions.

**Step 1 - verify the data.**

```bash
python FilteredFeatures.py
```

You won't normally run this alone; it's a library used by the next steps. But running it once confirms your CSV path and player IDs are correct.
To change which players or which behavior groups are included, edit the constants at the top of `FilteredFeatures.py`:

- `CSV_PATH` - path to the telemetry file
- `INCLUDE_PLAYER_IDS` - list of player IDs to keep
- `FILTER_VALUES` - which geometry behavior bins to include

**Step 2 - run the main visualization.**

```bash
python DataVisualizer.py
```

This:

1. Loads the CSV via FilteredFeatures
2. Runs Archetypal Analysis via ClusterProject (default K=8)
3. Opens an interactive matplotlib window with the cluster plot and side panels (behaviors, sessions, PCA interpretation)
4. Clicking a behavior in the side panel pops up the map preview from `icons/`
5. Clicking a data point looks for a matching replay video in `MyRecordings/`

You'll want to make sure `icons/` and `MyRecordings/` exist alongside the script for the click-through features to work, they arent needed but nice.

### Alternative visualizer (ExplorationVisualizer)

**Inputs:**

- `Telemetry_Raw.csv` in the repository root.

**Run:**

```bash
python ExplorationVisualizer.py
```

This is the 3D PCA plot with the interactive side panel, behaviors with marker shapes, sessions with color codes, and PCA interpretation text. Click a behavior to open its icon; click a point to open the replay video.
To configure: edit the constants near the top of the file (`N_CLUSTERS`, `INCLUDE_PLAYER_IDS`, `INCLUDE_FEATURES`).

### Per-player statistical comparison (CompareBehaviorDiff)

**Inputs:**

- Same as DataVisualizer (uses ClusterProject which uses FilteredFeatures, so it needs `Telemetry_RawControl.csv`).

**Run:**

```bash
python CompareBehaviorDiff.py
```

Prints to the terminal - per-player Wilcoxon test results comparing intra-player telemetry distances when behavior is the same vs. when behavior differs.

## Other Pyton scripts

All of theese scripts are located under the `Old/`

### (Scree)

**Inputs:**

- Same `Telemetry_RawControl.csv` as the main pipeline.

**Run:**

```bash
python Scree.py
```

Runs Archetypal Analysis for K = 1 through 20, prints sum of squares for each, then opens a scree plot. Find the in the curve to pick a good K. The chosen K then goes into `ClusterProject.py` under `ANALYSIS_KWARGS["n_archetypes"]`.

### Statistical tests (KRW / MUI / MixedEffects / Wilxocon)

These four scripts run different statistical tests on `Telemetry_Raw.csv` to check whether behavior parameters affect telemetry features. Each just prints its results.

| Script            | What it does                                                  | Run                      |
| ----------------- | ------------------------------------------------------------- | ------------------------ |
| `KRW.py`          | Kruskal-Wallis test across behavior groups                    | `python KRW.py`          |
| `MUI.py`          | Mutual information between behavior params and telemetry      | `python MUI.py`          |
| `MixedEffects.py` | Linear mixed-effects regression (random intercept per player) | `python MixedEffects.py` |
| `Wilxocon.py`     | Wilcoxon signed-rank across level configs                     | `python Wilxocon.py`     |

All four need `Telemetry_Raw.csv` in the repository root.

---

## Full end-to-end workflow (typical playtest)

If you're running a complete cycle from "I want to train a new model" to "I have analysis ready":

1. **Generate maps in Unity** -> open `CMA_ME.unity`, press Play, wait.
2. **Move the enemy archive** from `Assets/` to `Assets/StreamingAssets/`.
3. **Select a diverse subset** -> open `MapSelectorScene.unity`, set the input file, press Play.
4. **Render map previews** -> copy or rename `diverse_maps.json` to `maps.json` in repo root, then `python ArchiveRunner.py`.
5. **Build and ship the game** to playtesters; collect telemetry CSVs.
6. **Drop the CSVs in the repo root** as `Telemetry_RawControl.csv` (for clustering) or `Telemetry_Raw.csv` (for stats).
7. **Analyze** -> run `python DataVisualizer.py` for clusters, `python ExplorationVisualizer.py` for the 3D plot, plus whichever stats tests you need.
