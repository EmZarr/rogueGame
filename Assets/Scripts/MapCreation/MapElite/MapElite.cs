using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

public class MapElite : MonoBehaviour
{

    [Header("MAP-Elites Parameters")]
    [SerializeField] protected int totalIterations = 50;       // I
    [SerializeField] protected int initialRandomSolutions = 20; // G
    [SerializeField] protected TrainingLogger trainingLogger; // G

    protected Dictionary<Vector2, Map> geoArchive = new Dictionary<Vector2, Map>();
    protected Dictionary<(Vector2, Vector2), Map> furnArchive = new Dictionary<(Vector2, Vector2), Map>();
    protected Dictionary<(Vector2, Vector2, Vector2), Map> enemArchive = new Dictionary<(Vector2, Vector2, Vector2), Map>();

    [ThreadStatic] private static System.Random _rng;
    private static System.Random Rng => _rng ??= new System.Random();


    protected void Awake()
    {
        //Debug.unityLogger.logEnabled = false;
    }
    protected virtual void Start()
    {
        RunMapElitesGeometry();
        MapJsonExporter.SaveMaps(geoArchive.Values.ToList(), "geoArchive_maps.json");

        RunMapElitesFurnishing();
        MapJsonExporter.SaveMaps(furnArchive.Values.ToList(), "furnArchive_maps.json");

        RunMapElitesEnemies();
        MapJsonExporter.SaveMaps(enemArchive.Values.ToList(), "enemArchive_maps.json");
    }


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

            var (fitness, behavior) = GeoFitAndBehav.GetGeoFitnessAndBehavior(candidate);
            candidate.geoBehavior = new Vector2Int(behavior, 0);
            candidate.geoFitness = fitness;

            safeArchive.AddOrUpdate(
                candidate.geoBehavior,
                candidate,
                (key, existing) => candidate.CombinedFitness > existing.CombinedFitness ? candidate : existing
            );



            int completed = System.Threading.Interlocked.Increment(ref completedIterations);
            if (trainingLogger != null && completed % trainingLogger.LogEveryNIterations == 0)
                trainingLogger.LogGeometry(completed, new Dictionary<Vector2, Map>(safeArchive));
        });

        foreach (var kvp in safeArchive)
            geoArchive[kvp.Key] = kvp.Value;

        
    }

    public void RunMapElitesEnemies()
    {

        for (int i = 0; i < totalIterations * 3; i++)
        {
            // Generate candidate
            Map candidate;
            if (i <= initialRandomSolutions)
            {
                candidate = GenerateRandomEnemies(SelectRandom(furnArchive));
            }
            else
            {
                Map parent = SelectRandom(enemArchive);
                candidate = MutateEnemies(parent);
            }

            //behavior + fitness
            var (fitness, behavior) = EnemFitAndBehav.GetEnemyFitnessAndBehavior(candidate);
            candidate.enemyBehavior = new Vector2Int(behavior.enemyType, behavior.difficulty);
            candidate.enemFitness = fitness;

            // Store candidate + track delta on overwrite
            var key = (candidate.geoBehavior, candidate.furnBehavior, candidate.enemyBehavior);

            if (!enemArchive.TryGetValue(key, out var prev))
            {
                enemArchive[key] = candidate;
            }
            else if (candidate.CombinedFitness > prev.CombinedFitness)
            {
                enemArchive[key] = candidate;
            }
            trainingLogger?.LogEnemies(i, enemArchive);

        }
    }


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

        //trainingLogger?.LogFurnishing(totalIterations, furnArchive);
    }

    protected static Map GenerateRandomGeometry()
    {
        /*MapCandidate candidate = new MapCandidate(mapGenerator.MakeMap());
        return candidate;*/
        var map = new Map();
        GeometryGenerator.CreateMapGeometry(map);
        GeometryGenerator.BuildRoomTopology(map);
        return new Map(map);

    }

    protected static Map GenerateRandomEnemies(Map parent)
    {
        var child = parent.Clone();
        ObjectPlacementGenerator.CreateEnemiesOnMap(child);
        return child;
    }
    protected static Map GenerateRandomFurnishing(Map parent)
    {
        var child = parent.Clone();
        ObjectPlacementGenerator.CreateLootOnMap(child);
        ObjectPlacementGenerator.CreateObstaclesOnMap(child);
        return child;
    }
    protected static Map SelectRandom<TKey>(Dictionary<TKey, Map> archive)
    {
        var list = archive.Values.ToList();
        return list[Rng.Next(0, list.Count)];
    }

    protected static Map MutateGeometry(Map parent)
    {
        var child = parent.Clone();
        GeometryGenerator.MutateMapGeometry(child);
        GeometryGenerator.BuildRoomTopology(child);
        return child;
    }

    protected static Map MutateEnemies(Map parent)
    {
        var child = parent.Clone();
        ObjectPlacementGenerator.MutateEnemies(child);
        return child;
    }

    protected static Map MutateFurnishing(Map parent)
    {
        var child = parent.Clone();
        ObjectPlacementGenerator.MutateLoot(child);
        ObjectPlacementGenerator.MutateObstacles(child);
        return child;
    }
}
