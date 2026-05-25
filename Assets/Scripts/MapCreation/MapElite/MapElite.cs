using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


// Runs three sequential stages, each one a parallel for-loop:
//   1. RunMapElitesGeometry
//   2. RunMapElitesFurnishing 
//   3. RunMapElitesEnemies

// Each stage:
//   - first `initialRandomSolutions` candidates are random
//   - remaining candidates are mutations of a random archive elite
//   - a candidate replaces an existing cell only if its
//     CombinedFitness (geo + furn + enem) is higher

// Output: three JSON archives

public class MapElite : MonoBehaviour
{
    [Header("MAP-Elites Parameters")]
    [SerializeField] protected int totalIterations = 50;       // I
    [SerializeField] protected int initialRandomSolutions = 20; // G
    [SerializeField] protected TrainingLogger trainingLogger;

    // Keys grow in dimension because each stage refines the previous one's elites.
    protected Dictionary<Vector2, Map> geoArchive = new Dictionary<Vector2, Map>();
    protected Dictionary<(Vector2, Vector2), Map> furnArchive = new Dictionary<(Vector2, Vector2), Map>();
    protected Dictionary<(Vector2, Vector2, Vector2), Map> enemArchive = new Dictionary<(Vector2, Vector2, Vector2), Map>();

    // Thread-local RNG so parallel workers don't fight over a shared instance.
    [ThreadStatic] private static System.Random _rng;
    private static System.Random Rng => _rng ??= new System.Random();

    // Runs the three stages in sequence.
    protected virtual void Start()
    {
        RunMapElitesGeometry();
        MapJsonExporter.SaveMaps(geoArchive.Values.ToList(), "geoArchive_maps.json");

        RunMapElitesFurnishing();
        MapJsonExporter.SaveMaps(furnArchive.Values.ToList(), "furnArchive_maps.json");

        RunMapElitesEnemies();
        MapJsonExporter.SaveMaps(enemArchive.Values.ToList(), "enemArchive_maps.json");
    }

    // geoBehavior (openness bin in [0, 12]).
    // First initialRandomSolutions candidates are random, the rest are mutations of a randomly-chosen archive elite.
 
    // Runs in parallel via Parallel.For.
    // results are copied into geoArchive at the end.
    public void RunMapElitesGeometry()
    {
        var safeArchive = new ConcurrentDictionary<Vector2, Map>();
        int completedIterations = 0;

        var options = new System.Threading.Tasks.ParallelOptions
        {
            MaxDegreeOfParallelism = System.Environment.ProcessorCount - 2 // leave some cores free
        };

        System.Threading.Tasks.Parallel.For(0, totalIterations, options, i =>
        {
            // Snapshot so SelectRandom sees a stable archive even if another worker mutates mid-iteration.
            var snapshot = new Dictionary<Vector2, Map>(safeArchive);
            Map candidate;
            if (i <= initialRandomSolutions || snapshot.Count == 0)
            {
                candidate = GenerateRandomGeometry();
            }
            else
            {
                Map parent = SelectRandom(new Dictionary<Vector2, Map>(safeArchive));
                candidate = MutateGeometry(parent);
            }

            // Evaluate behavior + fitness
            var (fitness, behavior) = GeoFitAndBehav.GetGeoFitnessAndBehavior(candidate);
            candidate.geoBehavior = new Vector2Int(behavior, 0);
            candidate.geoFitness = fitness;

            // Replace the existing elite in this cell only if the new candidate has a higher CombinedFitness.
            safeArchive.AddOrUpdate(
                candidate.geoBehavior,
                candidate,
                (key, existing) => candidate.CombinedFitness > existing.CombinedFitness ? candidate : existing
            );



            int completed = System.Threading.Interlocked.Increment(ref completedIterations);
            if (trainingLogger != null && completed % trainingLogger.LogEveryNIterations == 0)
                trainingLogger.LogGeometry(completed, new Dictionary<Vector2, Map>(safeArchive));
        });

        // Copy thread-safe archive back into the regular dictionary
        foreach (var kvp in safeArchive)
            geoArchive[kvp.Key] = kvp.Value;

        
    }

    // Runs totalIterations * 3 iterations because the enemy behavior space is much larger (126 composition bins × 3 difficulty bins per (geo, furn) cell).

    // First-time candidates clone a parent from the furn archive (since this part adds the enemy layer).
    // Later candidates mutate enemies on an existing enemy elite.
    public void RunMapElitesEnemies()
    {
        int completedEnemies = 0;

        var safeArchive = new ConcurrentDictionary<(Vector2, Vector2, Vector2), Map>();

        var options = new System.Threading.Tasks.ParallelOptions
        {
            MaxDegreeOfParallelism = Math.Max(1, System.Environment.ProcessorCount - 2)
        };

        System.Threading.Tasks.Parallel.For(0, totalIterations * 3, options, i =>
        {
            Map candidate;

            if (i <= initialRandomSolutions || safeArchive.Count == 0)
            {
                // Use furnishing archive as the base for initial enemy generation
                Map parent = SelectRandom(new Dictionary<(Vector2, Vector2), Map>(furnArchive));
                candidate = GenerateRandomEnemies(parent);
            }
            else
            {
                // Select from the thread-safe local enemy archive
                Map parent = SelectRandom(new Dictionary<(Vector2, Vector2, Vector2), Map>(safeArchive));
                candidate = MutateEnemies(parent);
            }

            // Behavior + fitness
            var (fitness, behavior) = EnemFitAndBehav.GetEnemyFitnessAndBehavior(candidate);

            candidate.enemyBehavior = new Vector2Int(
                behavior.enemyType,
                behavior.difficulty
            );

            candidate.enemFitness = fitness;

            // Store candidate if the cell is empty or if this candidate is better
            safeArchive.AddOrUpdate(
                (candidate.geoBehavior, candidate.furnBehavior, candidate.enemyBehavior),
                candidate,
                (key, existing) =>
                    candidate.CombinedFitness > existing.CombinedFitness
                        ? candidate
                        : existing
            );

            int completed = System.Threading.Interlocked.Increment(ref completedEnemies);

            if (trainingLogger != null &&
                completed % trainingLogger.LogEveryNIterations == 0)
            {
                trainingLogger.LogEnemies(
                    completed,
                    new Dictionary<(Vector2, Vector2, Vector2), Map>(safeArchive)
                );
            }
        });

        // Copy local parallel archive back into the main enemy archive
        foreach (var kvp in safeArchive)
        {
            enemArchive[kvp.Key] = kvp.Value;
        }

    }

    // Behavior key: (geoBehavior, furnBehavior).
    // Seeds clone a parent from the geo archive.
    // Later candidates mutate an existing furnishing elite.
    public void RunMapElitesFurnishing()
    {
        int completedFurn = 0;
        var safeArchive = new ConcurrentDictionary<(Vector2, Vector2), Map>();

        var options = new System.Threading.Tasks.ParallelOptions
        {
            MaxDegreeOfParallelism = System.Environment.ProcessorCount - 2
        };

        System.Threading.Tasks.Parallel.For(0, totalIterations, options, i =>
        {
            Map candidate;
            if (i <= initialRandomSolutions || safeArchive.Count == 0)
            {
                Map parent = SelectRandom(new Dictionary<Vector2, Map>(geoArchive));
                candidate = GenerateRandomFurnishing(parent);
            }
            else
            {
                Map parent = SelectRandom(new Dictionary<(Vector2, Vector2), Map>(safeArchive));
                candidate = MutateFurnishing(parent);
            }

            var (fitness, behavior) = FurnFitAndBehav.GetFurnFitnessAndBehavior(candidate);
            candidate.furnBehavior = new Vector2Int(behavior.lootDensity, behavior.obstacleDensity);
            candidate.furnFitness = fitness;

            safeArchive.AddOrUpdate(
                (candidate.geoBehavior, candidate.furnBehavior),
                candidate,
                (key, existing) => candidate.CombinedFitness > existing.CombinedFitness ? candidate : existing
            );

            int completed = System.Threading.Interlocked.Increment(ref completedFurn);
            if (trainingLogger != null && completed % trainingLogger.LogEveryNIterations == 0)
                trainingLogger.LogFurnishing(completed, new Dictionary<(Vector2, Vector2), Map>(safeArchive));

        });

        foreach (var kvp in safeArchive)
            furnArchive[kvp.Key] = kvp.Value;
    }

    // Creates a fresh random room layout from nothing.
    // Used as a seed in the geometry stage.
    protected static Map GenerateRandomGeometry()
    {
        var map = new Map();
        GeometryGenerator.CreateMapGeometry(map);
        GeometryGenerator.BuildRoomTopology(map);
        return map;
    }

    // Clones a furnished parent and adds fresh enemies to the clone.
    // Used as a seed in the enemy stage.
    protected static Map GenerateRandomEnemies(Map parent)
    {
        var child = parent.Clone();
        ObjectPlacementGenerator.CreateEnemiesOnMap(child);
        return child;
    }

    // Clones a geometry parent and adds fresh loot + obstacles.
    // Used as a seed in the furnishing stage.
    protected static Map GenerateRandomFurnishing(Map parent)
    {
        var child = parent.Clone();
        ObjectPlacementGenerator.CreateLootOnMap(child);
        ObjectPlacementGenerator.CreateObstaclesOnMap(child);
        return child;
    }

    // Picks a uniformly random map from the archive.

    // Avoids allocating a ToList() by iterating the dictionary's Values and stopping at the chosen index.
    // Worth it because this is called once per non-seed iteration in every stage.
    protected static Map SelectRandom<TKey>(Dictionary<TKey, Map> archive) //we do a little optimal selecting
    {
        int index = Rng.Next(0, archive.Count);
        int i = 0;
        foreach (var value in archive.Values)
        {
            if (i == index) return value;
            i++;
        }
        return null;
    }

    // Clones a parent, mutates its room layout (chunks added/removed), then rebuilds room topology so entry/exit tiles and main path are updated.
    protected static Map MutateGeometry(Map parent)
    {
        var child = parent.Clone();
        GeometryGenerator.MutateMapGeometry(child);
        GeometryGenerator.BuildRoomTopology(child);
        return child;
    }

    // Clones a parent and mutates its enemy placements (some budgets re-rolled, some enemies swapped).
    protected static Map MutateEnemies(Map parent)
    {
        var child = parent.Clone();
        ObjectPlacementGenerator.MutateEnemies(child);
        return child;
    }

    // Clones a parent and mutates loot + obstacle placements.
    protected static Map MutateFurnishing(Map parent)
    {
        var child = parent.Clone();
        ObjectPlacementGenerator.MutateLoot(child);
        ObjectPlacementGenerator.MutateObstacles(child);
        return child;
    }
}
