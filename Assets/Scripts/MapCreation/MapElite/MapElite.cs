using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using System.Text;

public class MapElite : MonoBehaviour
{

    [Header("MAP-Elites Parameters")]
    [SerializeField] protected int totalIterations = 50;       // I
    [SerializeField] int initialRandomSolutions = 20; // G

    protected Dictionary<Vector2, Map> geoArchive = new Dictionary<Vector2, Map>();
    protected Dictionary<(Vector2, Vector2), Map> furnArchive = new Dictionary<(Vector2, Vector2), Map>();
    protected Dictionary<(Vector2, Vector2, Vector2), Map> enemArchive = new Dictionary<(Vector2, Vector2, Vector2), Map>();

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
        int iter = 0;
        // Delta only for replacements (overwrites), averaged per log interval
        float sumDeltaCombined = 0f;
        int deltaCount = 0;

        const float geoThreshold = 0.8f;
        const int logEvery = 500;

        string path = Path.Combine(Application.dataPath, "GeoFitness.csv");
        var sb = new StringBuilder();
        sb.AppendLine("iterations, archive avg geometry fitness, archive avg total fitness, avg delta total fitness, elites total, elites geoFitness above 0.8");

        for (int i = 0; i < totalIterations; i++)
        {
            // --- Generate candidate ---
            Map candidate;
            if (iter <= initialRandomSolutions)
            {
                candidate = GenerateRandomGeometry();
                iter++;
            }
            else
            {
                //MapCandidate parent = SelectRandomGeometry(); OLD
                Map parent = SelectRandom(geoArchive);
                candidate = MutateGeometry(parent);
            }

            // behavior + fitness
            var (fitness, behavior) = GeoFitAndBehav.GetGeoFitnessAndBehavior(candidate);
            candidate.geoBehavior = new Vector2Int(behavior, 0);
            candidate.geoFitness = fitness;

            // Store candidate + track delta on overwrite
            var key = candidate.geoBehavior;
            if (!geoArchive.TryGetValue(key, out var prev))
            {
                geoArchive[key] = candidate;
            }

            else if (candidate.CombinedFitness > prev.CombinedFitness)
            {
                float delta = candidate.CombinedFitness - prev.CombinedFitness;
                geoArchive[key] = candidate;

                if (!float.IsNaN(delta) && !float.IsInfinity(delta))
                {
                    sumDeltaCombined += delta;
                    deltaCount++;
                }
            }

            //Log
            if ((i > 0 && i % logEvery == 0) || i == totalIterations - 1)
            {
                int elitesTotal = geoArchive.Count;

                float archiveAvgGeo = (elitesTotal > 0) ? geoArchive.Values.Average(e => e.geoFitness) : 0f;
                float archiveAvgTotal = (elitesTotal > 0) ? geoArchive.Values.Average(e => e.CombinedFitness) : 0f;

                int elitesAboveGeoThreshold = geoArchive.Values.Count(e => e.geoFitness > geoThreshold);

                float avgDelta = (deltaCount > 0) ? (sumDeltaCombined / deltaCount) : 0f;

                sb.AppendLine($"{i}, {archiveAvgGeo}, {archiveAvgTotal}, {avgDelta}, {elitesTotal}, {elitesAboveGeoThreshold}");

                sumDeltaCombined = 0f;
                deltaCount = 0;
            }
        }

        File.WriteAllText(path, sb.ToString());
    }




    public void RunMapElitesEnemies()
    {
        int iter = 0;

        // Delta only for replacements (overwrites), averaged per log interval
        float sumDeltaCombined = 0f;
        int deltaCount = 0;

        const float enemThreshold = 0.8f;

        const int logEvery = 500;

        string path = Path.Combine(Application.dataPath, "EneFitness.csv");
        var sb = new StringBuilder();
        sb.AppendLine("iterations, archive avg enemy fitness, archive avg total fitness, avg delta total fitness, elites total, elites enemFitness above 0.8");

        for (int i = 0; i < totalIterations * 3; i++)
        {
            // Generate candidate
            Map candidate;
            if (iter <= initialRandomSolutions)
            {
                candidate = GenerateRandomEnemies(SelectRandom(furnArchive));
                iter++;
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
                float delta = candidate.CombinedFitness - prev.CombinedFitness;
                enemArchive[key] = candidate;

                if (!float.IsNaN(delta) && !float.IsInfinity(delta))
                {
                    sumDeltaCombined += delta;
                    deltaCount++;
                }
            }

            // Log
            if ((i > 0 && i % logEvery == 0) || i == totalIterations - 1)
            {
                int elitesTotal = enemArchive.Count;

                float archiveAvgEne = (elitesTotal > 0) ? enemArchive.Values.Average(e => e.enemFitness) : 0f;
                float archiveAvgTotal = (elitesTotal > 0) ? enemArchive.Values.Average(e => e.CombinedFitness) : 0f;

                int elitesAboveThreshold = enemArchive.Values.Count(e => e.enemFitness > enemThreshold);

                float avgDelta = (deltaCount > 0) ? (sumDeltaCombined / deltaCount) : 0f;

                sb.AppendLine($"{i}, {archiveAvgEne}, {archiveAvgTotal}, {avgDelta}, {elitesTotal}, {elitesAboveThreshold}");

                sumDeltaCombined = 0f;
                deltaCount = 0;
            }
        }

        File.WriteAllText(path, sb.ToString());
    }


    public void RunMapElitesFurnishing()
    {
        int iter = 0;

        // Delta only for replacements (overwrites), averaged per log interval
        float sumDeltaCombined = 0f;
        int deltaCount = 0;

        const float furnThreshold = 0.8f;

        const int logEvery = 500;

        string path = Path.Combine(Application.dataPath, "FurFitness.csv");
        var sb = new StringBuilder();
        sb.AppendLine("iterations, archive avg furnish fitness, archive avg total fitness, avg delta total fitness, elites total, elites furnFitness above 0.8");

        for (int i = 0; i < totalIterations; i++)
        {
            // Generate candidate
            Map candidate;
            if (iter <= initialRandomSolutions)
            {
                candidate = GenerateRandomFurnishing(SelectRandom(geoArchive));
                iter++;
            }
            else
            {
                Map parent = SelectRandom(furnArchive);
                candidate = MutateFurnishing(parent);
            }

            // behavior + fitness 
            var (fitness, behavior) = FurnFitAndBehav.GetFurnFitnessAndBehavior(candidate);
            candidate.furnBehavior = new Vector2Int(behavior.lootDensity, behavior.obstacleDensity);
            candidate.furnFitness = fitness;

            // Store candidate + track delta on overwrite
            var key = (candidate.geoBehavior, candidate.furnBehavior);

            if (!furnArchive.TryGetValue(key, out var prev))
            {
                furnArchive[key] = candidate;
            }
            else if (candidate.CombinedFitness > prev.CombinedFitness)
            {
                float delta = candidate.CombinedFitness - prev.CombinedFitness;
                furnArchive[key] = candidate;

                if (!float.IsNaN(delta) && !float.IsInfinity(delta))
                {
                    sumDeltaCombined += delta;
                    deltaCount++;
                }
            }

            // Log 
            if ((i > 0 && i % logEvery == 0) || i == totalIterations - 1)
            {
                int elitesTotal = furnArchive.Count;

                float archiveAvgFurn = (elitesTotal > 0) ? furnArchive.Values.Average(e => e.furnFitness) : 0f;
                float archiveAvgTotal = (elitesTotal > 0) ? furnArchive.Values.Average(e => e.CombinedFitness) : 0f;

                int elitesAboveThreshold = furnArchive.Values.Count(e => e.furnFitness > furnThreshold);

                float avgDelta = (deltaCount > 0) ? (sumDeltaCombined / deltaCount) : 0f;

                sb.AppendLine($"{i}, {archiveAvgFurn}, {archiveAvgTotal}, {avgDelta}, {elitesTotal}, {elitesAboveThreshold}");

                sumDeltaCombined = 0f;
                deltaCount = 0;
            }
        }

        File.WriteAllText(path, sb.ToString());
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
        return list[Random.Range(0, list.Count)];
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
