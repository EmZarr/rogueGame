using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using System.Text;

public class MapElite : MonoBehaviour
{

    [Header("MAP-Elites Parameters")]
    [SerializeField] int totalIterations = 50;       // I
    [SerializeField] int initialRandomSolutions = 20; // G

    MapGenerator mapGenerator;

    private Dictionary<Vector2, MapCandidate> geoArchive = new Dictionary<Vector2, MapCandidate>();
    private Dictionary<(Vector2, Vector2), MapCandidate> furnArchive = new Dictionary<(Vector2, Vector2), MapCandidate>();
    private Dictionary<(Vector2, Vector2, Vector2), MapCandidate> enemArchive = new Dictionary<(Vector2, Vector2, Vector2), MapCandidate>();

    private void Awake()
    {
        //Debug.unityLogger.logEnabled = false;
    }
    void Start()
    {

        mapGenerator = GetComponent<MapGenerator>();

        RunMapElitesGeometry();
        MapArchiveExporter.ExportArchiveToJson(geoArchive.Values, "geoArchive_maps.json");

        RunMapElitesFurnishing();
        MapArchiveExporter.ExportArchiveToJson(furnArchive.Values, "furnArchive_maps.json");

        RunMapElitesEnemies();
        MapArchiveExporter.ExportArchiveToJson(enemArchive.Values, "enemArchive_maps.json");
      
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
                //MapCandidate parent = SelectRandomGeometry(); OLD
                MapCandidate parent = SelectRandom(geoArchive);
                candidate = MutateGeometry(parent);
            }

            // behavior + fitness
            candidate.geoBehavior = new Vector2(
                BehaviorFunctions.GetComponentCountBehavior(candidate.mapData),
                0
            );
            candidate.geoFitness = FitnessFunctions.GetGeometryFitness(candidate);

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
            MapCandidate candidate;
            if (iter <= initialRandomSolutions)
            {
                candidate = GenerateRandomEnemies(SelectRandom(furnArchive));
                iter++;
            }
            else
            {
                MapCandidate parent = SelectRandom(enemArchive);
                candidate = MutateEnemies(parent);
            }

            //behavior + fitness
            candidate.enemyBehavior = new Vector2(BehaviorFunctions.EnemyRoleCompositionBehavior(candidate.mapData.enemies, 20), BehaviorFunctions.EnemyDifficultyBehavior(candidate.mapData));

            candidate.enemFitness = FitnessFunctions.GetEnemyFitness(candidate);

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
            MapCandidate candidate;
            if (iter <= initialRandomSolutions)
            {
                candidate = GenerateRandomFurnishing(SelectRandom(geoArchive));
                iter++;
            }
            else
            {
                MapCandidate parent = SelectRandom(furnArchive);
                candidate = MutateFurnishing(parent);
            }

            // behavior + fitness 
            candidate.furnBehavior = new Vector2(BehaviorFunctions.FurnishingBehaviorExploration(candidate.mapData), BehaviorFunctions.FurnishingBehaviorSafety(candidate.mapData));

            candidate.furnFitness = FitnessFunctions.GetFurnishingFitness(candidate);

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

    MapCandidate GenerateRandomGeometry()
    {
        /*MapCandidate candidate = new MapCandidate(mapGenerator.MakeMap());
        return candidate;*/
        var map = new Map();
        GeometryGenerator.CreateMapGeometry(map);
        GeometryGenerator.BuildRoomTopology(map);
        return new MapCandidate(map);

    }

    MapCandidate GenerateRandomEnemies(MapCandidate parent)
    {
        var child = new MapCandidate(parent.mapData.Clone());
        ObjectPlacementGenerator.CreateEnemiesOnMap(child.mapData);

        child.geoBehavior = parent.geoBehavior;
        child.geoFitness = parent.geoFitness;
        child.furnBehavior = parent.furnBehavior;
        child.furnFitness = parent.furnFitness;

        return child;
    }

    MapCandidate GenerateRandomFurnishing(MapCandidate parent)
    {
        var child = new MapCandidate(parent.mapData.Clone());
        ObjectPlacementGenerator.CreateLootOnMap(child.mapData);
        ObjectPlacementGenerator.CreateObstaclesOnMap(child.mapData);

        child.geoBehavior = parent.geoBehavior;
        child.geoFitness = parent.geoFitness;

        return child;
    }
    /*
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
    }*/

MapCandidate SelectRandom<TKey>(Dictionary<TKey, MapCandidate> archive)
{
    var list = archive.Values.ToList();
    return list[Random.Range(0, list.Count)];
}

    MapCandidate MutateGeometry(MapCandidate parent)
    {
        /*var child = new MapCandidate(parent.mapData.Clone());
        child.mapData = mapGenerator.mutateGeometry(child.mapData);
        return child;*/

        var child = new MapCandidate(parent.mapData.Clone());
        GeometryGenerator.MutateMapGeometry(child.mapData);
        GeometryGenerator.BuildRoomTopology(child.mapData);
        return child;
    }

    MapCandidate MutateEnemies(MapCandidate parent)
    {
        var child = new MapCandidate(parent.mapData.Clone());

        /*child.geoBehavior = new Vector2(parent.geoBehavior.x, parent.geoBehavior.y);
        child.geoFitness = parent.geoFitness;
        child.furnBehavior = new Vector2(parent.furnBehavior.x, parent.furnBehavior.y);
        child.furnFitness = parent.furnFitness;
        child.mapData = mapGenerator.mutateEnemies(child.mapData);*/

        ObjectPlacementGenerator.MutateLoot(child.mapData);
        ObjectPlacementGenerator.MutateObstacles(child.mapData);

        child.geoBehavior = parent.geoBehavior;
        child.geoFitness = parent.geoFitness;

        return child;
    }

    MapCandidate MutateFurnishing(MapCandidate parent)
    {
        var child = new MapCandidate(parent.mapData.Clone());
        ObjectPlacementGenerator.MutateLoot(child.mapData);
        ObjectPlacementGenerator.MutateObstacles(child.mapData);

        /*child.geoBehavior = new Vector2(parent.geoBehavior.x, parent.geoBehavior.y);
        child.geoFitness = parent.geoFitness;*/

        child.geoBehavior = parent.geoBehavior;
        child.geoFitness = parent.geoFitness;

        return child;
    }
}

public class MapCandidate
{
    public float geoFitness;
    public float enemFitness;
    public float furnFitness;
    public Map mapData;

    public float CombinedFitness => geoFitness + enemFitness + furnFitness;
    // Behavior slices
    public Vector2 geoBehavior;
    public Vector2 furnBehavior;
    public Vector2 enemyBehavior;

    public MapCandidate(Map map)
    {
        mapData = map;
        geoFitness = 0f;
        enemFitness = 0f;
        furnFitness = 0f;
        geoBehavior = new Vector2();
        furnBehavior = new Vector2();
        enemyBehavior = new Vector2();
    }
}