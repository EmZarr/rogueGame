using NUnit.Framework.Internal;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using System.IO;
using System.Text;

public class MapElite : MonoBehaviour
{

    [Header("MAP-Elites Parameters")]
    [SerializeField] int totalIterations = 50;       // I
    [SerializeField] int initialRandomSolutions = 20; // G


    [Header("Controls")]
    public InputActionReference runElites; 
    MapGenerator mapGenerator;

    private Dictionary<Vector2, MapCandidate> geoArchive = new Dictionary<Vector2, MapCandidate>();
    private Dictionary<(Vector2, Vector2), MapCandidate> enemArchive = new Dictionary<(Vector2, Vector2), MapCandidate>();
    private Dictionary<(Vector2, Vector2, Vector2), MapCandidate> furnArchive = new Dictionary<(Vector2, Vector2, Vector2), MapCandidate>();
    private Dictionary<(Vector2, Vector2, Vector2), MapCandidate> combinedArchive = new Dictionary<(Vector2, Vector2, Vector2), MapCandidate>();

    private void Awake()
    {
        //Debug.unityLogger.logEnabled = false;
    }
    void Start()
    {

        runElites.action.Enable();
        runElites.action.performed += ctx => RunMapElitesGeometry();
        mapGenerator = GetComponent<MapGenerator>();
        RunMapElitesGeometry();
        MapArchiveExporter.ExportArchiveToJson(geoArchive.Values, "geoArchive_maps.json");
        RunMapElitesEnemies();
        MapArchiveExporter.ExportArchiveToJson(enemArchive.Values, "enemArchive_maps.json");
        RunMapElitesFurnishing();
        MapArchiveExporter.ExportArchiveToJson(furnArchive.Values, "furnArchive_maps.json");
        //RunMapElitesCombined();
        //MapArchiveExporter.ExportArchiveToJson(combinedArchive.Values, "combArchive_maps.json");
        
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
            MapCandidate candidate;
            if (iter <= initialRandomSolutions)
            {
                candidate = GenerateRandomGeometry();
                iter++;
            }
            else
            {
                MapCandidate parent = SelectRandomGeometry();
                candidate = MutateGeometry(parent);
            }

            // behavior + fitness
            candidate.geoBehavior = new Vector2(
                BehaviorFunctions.GetMapOpennessBehavior(candidate, 10),
                BehaviorFunctions.GetWindingnessBehavior(candidate, 10)
            );

            candidate.geoFitness = FitnessFunctions.GetGeometryFitness(
                candidate,
                (50, 10000, 0.35f),
                (0.5f, 2f, 0.1f),
                (1000, 3000, 0.35f),
                (2, 40, 0.1f),
                (0f, 0.15f, 0.1f)
            );

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

            // Log
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

        for (int i = 0; i < totalIterations; i++)
        {
            // Generate candidate
            MapCandidate candidate;
            if (iter <= initialRandomSolutions)
            {
                candidate = GenerateRandomEnemies(SelectRandomGeometry());
                iter++;
            }
            else
            {
                MapCandidate parent = SelectRandomEnemies();
                candidate = MutateEnemies(parent);
            }

            //´behavior + fitness
            candidate.enemyBehavior = BehaviorFunctions.EnemyClusterBehavior(
                candidate.mapData,
                BehaviorFunctions.EnemyCombatMix(candidate.mapData.enemies, Vector2.zero)
            );

            candidate.enemFitness = FitnessFunctions.EnemyFitnessTotal(candidate.mapData, 0.5f, 0.5f);

            // Store candidate + track delta on overwrite
            var key = (candidate.geoBehavior, candidate.enemyBehavior);

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
            MapCandidate candidate;
            if (iter <= initialRandomSolutions)
            {
                candidate = GenerateRandomFurnishing(SelectRandomEnemies());
                iter++;
            }
            else
            {
                MapCandidate parent = SelectRandomFurnishing();
                candidate = MutateFurnishing(parent);
            }

            // behavior + fitness 
            candidate.furnBehavior = BehaviorFunctions.FurnishingBehaviorPickupDanger(
                candidate.mapData,
                BehaviorFunctions.FurnishingBehaviorExploration(candidate.mapData, Vector2.zero)
            );

            candidate.furnFitness = FitnessFunctions.LootAtEndFitness(candidate.mapData);

            // Store candidate + track delta on overwrite
            var key = (candidate.geoBehavior, candidate.enemyBehavior, candidate.furnBehavior);

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


    public void RunMapElitesCombined()
    {
        int iter = 0;

        // Delta only for replacements (overwrites), averaged per log interval
        float sumDeltaCombined = 0f;
        int deltaCount = 0;

        const float combinedThreshold = 2.4f;

        const int logEvery = 500;

        string path = Path.Combine(Application.dataPath, "CombFitness.csv");
        var sb = new StringBuilder();
        sb.AppendLine("iterations, archive avg total fitness, archive avg geo fitness, archive avg enemy fitness, archive avg furnish fitness, avg delta total fitness, elites total, elites totalFitness above 0.8");

        for (int i = 0; i < totalIterations; i++)
        {
            // Generate candidate
            MapCandidate candidate;
            if (iter <= initialRandomSolutions)
            {
                candidate = GenerateRandomFurnishing(GenerateRandomEnemies(GenerateRandomGeometry()));
                iter++;
            }
            else
            {
                MapCandidate parent = SelectRandomCombined();
                candidate = MutateFurnishing(MutateEnemies(MutateGeometry(parent)));
            }

            // Behaviors
            candidate.geoBehavior = new Vector2(
                BehaviorFunctions.GetMapOpennessBehavior(candidate, 10),
                BehaviorFunctions.GetWindingnessBehavior(candidate, 10)
            );

            candidate.furnBehavior = BehaviorFunctions.FurnishingBehaviorPickupDanger(
                candidate.mapData,
                BehaviorFunctions.FurnishingBehaviorExploration(candidate.mapData, Vector2.zero)
            );

            candidate.enemyBehavior = BehaviorFunctions.EnemyClusterBehavior(
                candidate.mapData,
                BehaviorFunctions.EnemyCombatMix(candidate.mapData.enemies, Vector2.zero)
            );

            var key = candidate.CombinedBehavior;

            // Fitness
            candidate.geoFitness = FitnessFunctions.GetGeometryFitness(
                candidate,
                (50, 10000, 0.35f),
                (0.5f, 2f, 0.1f),
                (1000, 3000, 0.35f),
                (2, 40, 0.1f),
                (0f, 0.15f, 0.1f)
            );

            candidate.furnFitness = FitnessFunctions.FurnishingFitnessTotal(candidate.mapData, 0.34f, 0.33f, 0.33f);
            candidate.enemFitness = FitnessFunctions.EnemyFitnessTotal(candidate.mapData, 0.5f, 0.5f);

            // Store candidate + track delta on overwrite
            if (!combinedArchive.TryGetValue(key, out var prev))
            {
                combinedArchive[key] = candidate;
            }
            else if (candidate.CombinedFitness > prev.CombinedFitness)
            {
                float delta = candidate.CombinedFitness - prev.CombinedFitness;
                combinedArchive[key] = candidate;

                if (!float.IsNaN(delta) && !float.IsInfinity(delta))
                {
                    sumDeltaCombined += delta;
                    deltaCount++;
                }
            }

            // Log
            if ((i > 0 && i % logEvery == 0) || i == totalIterations - 1)
            {
                int elitesTotal = combinedArchive.Count;

                float archiveAvgTotal = (elitesTotal > 0) ? combinedArchive.Values.Average(e => e.CombinedFitness) : 0f;
                float archiveAvgGeo = (elitesTotal > 0) ? combinedArchive.Values.Average(e => e.geoFitness) : 0f;
                float archiveAvgEne = (elitesTotal > 0) ? combinedArchive.Values.Average(e => e.enemFitness) : 0f;
                float archiveAvgFurn = (elitesTotal > 0) ? combinedArchive.Values.Average(e => e.furnFitness) : 0f;

                int elitesAboveThreshold = combinedArchive.Values.Count(e => e.CombinedFitness > combinedThreshold);

                float avgDelta = (deltaCount > 0) ? (sumDeltaCombined / deltaCount) : 0f;

                sb.AppendLine($"{i}, {archiveAvgTotal}, {archiveAvgGeo}, {archiveAvgEne}, {archiveAvgFurn}, {avgDelta}, {elitesTotal}, {elitesAboveThreshold}");

                sumDeltaCombined = 0f;
                deltaCount = 0;
            }
        }

        File.WriteAllText(path, sb.ToString());
    }



    MapCandidate GenerateRandomGeometry()
    {   
        MapCandidate candidate = new MapCandidate(mapGenerator.MakeMap());
        return candidate;
    }

    MapCandidate GenerateRandomEnemies(MapCandidate parent)
    {
        var mapCopy = mapGenerator.placeEnemies(parent.mapData.Clone());
        var child = new MapCandidate(mapCopy);
        child.geoBehavior = parent.geoBehavior;
        child.geoFitness = parent.geoFitness;

        return child;
    }

    MapCandidate GenerateRandomFurnishing(MapCandidate parent)
    {
        var mapCopy = mapGenerator.placeFurnishing(parent.mapData.Clone());
        var child = new MapCandidate(mapCopy);
        child.geoBehavior = parent.geoBehavior;
        child.geoFitness = parent.geoFitness;
        child.enemyBehavior = parent.enemyBehavior;
        child.enemFitness = parent.enemFitness;

        return child;
    }
    MapCandidate SelectRandomGeometry()
    {
        //Return random solution in the archive
        return geoArchive.Values.ToList()[Random.Range(0, geoArchive.Count)];
    }

    MapCandidate SelectRandomFurnishing()
    {
        //Return random solution in the archive
        return furnArchive.Values.ToList()[Random.Range(0, furnArchive.Count)];
    }

    MapCandidate SelectRandomEnemies()
    {
        //Return random solution in the archive
        return enemArchive.Values.ToList()[Random.Range(0, enemArchive.Count)];
    }

    MapCandidate SelectRandomCombined()
    {
        //Return random solution in the archive
        return combinedArchive.Values.ToList()[Random.Range(0, combinedArchive.Count)];
    }

    MapCandidate MutateGeometry(MapCandidate parent)
    {
        var child = new MapCandidate(parent.mapData.Clone());
        child.mapData = mapGenerator.mutateGeometry(child.mapData);
        return child;
    }
    
     MapCandidate MutateEnemies(MapCandidate parent)
    {
        var child = new MapCandidate(parent.mapData.Clone());
        child.geoBehavior = new Vector2(parent.geoBehavior.x, parent.geoBehavior.y);
        child.geoFitness = parent.geoFitness;
        child.mapData = mapGenerator.mutateEnemies(child.mapData);
        return child;
    }

    MapCandidate MutateFurnishing(MapCandidate parent)
    {
        var child = new MapCandidate(parent.mapData.Clone());
        child.geoBehavior = new Vector2(parent.geoBehavior.x, parent.geoBehavior.y);
        child.geoFitness = parent.geoFitness;
        child.enemyBehavior = new Vector2(parent.enemyBehavior.x, parent.enemyBehavior.y);
        child.enemFitness = parent.enemFitness;
        child.mapData = mapGenerator.mutateFurnishing(child.mapData);
        return child;
    }
}

public class MapCandidate
{
    public float geoFitness;
    public float enemFitness;
    public float furnFitness;
    public MapInfo mapData;

    public float CombinedFitness => geoFitness + enemFitness + furnFitness;
    // Behavior slices
    public Vector2 geoBehavior;
    public Vector2 furnBehavior;
    public Vector2 enemyBehavior;

    // Combined key
    public (Vector2 geo, Vector2 furn, Vector2 enemy) CombinedBehavior
        => (geoBehavior, furnBehavior, enemyBehavior);

    public MapCandidate(MapInfo map)
    {
        mapData = map;
        geoFitness = 0f;
        enemFitness = 0f;
        furnFitness = 0f;
        geoBehavior = new Vector2() ;
        furnBehavior = new Vector2();
        enemyBehavior = new Vector2();
    }
}



