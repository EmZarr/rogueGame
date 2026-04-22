using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using System.Text;

public class MapElite : MonoBehaviour
{

    [Header("MAP-Elites Parameters")]
    [SerializeField] protected int totalIterations = 50;       // I
    [SerializeField] protected int initialRandomSolutions = 20; // G
    [SerializeField] protected TrainingLogger trainingLogger; // G

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
        for (int i = 0; i < totalIterations; i++)
        {
            // --- Generate candidate ---
            Map candidate;
            if (i <= initialRandomSolutions)
            {
                candidate = GenerateRandomGeometry();
            }
            else
            {
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
                geoArchive[key] = candidate;
            }
            trainingLogger?.LogGeometry(i, geoArchive);
        }
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
        for (int i = 0; i < totalIterations; i++)
        {
            // Generate candidate
            Map candidate;
            if (i <= initialRandomSolutions)
            {
                candidate = GenerateRandomFurnishing(SelectRandom(geoArchive));
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
                furnArchive[key] = candidate;
            }
            trainingLogger?.LogFurnishing(i, furnArchive);
        }
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
