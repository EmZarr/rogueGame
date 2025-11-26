using NUnit.Framework.Internal;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class MapElite : MonoBehaviour
{
    [SerializeField] MapInstantiator mapInstantiator;

    [Header("MAP-Elites Parameters")]
    [SerializeField] int totalIterations = 50;       // I
    [SerializeField] int initialRandomSolutions = 20; // G


    [Header("Controls")]
    public InputActionReference runElites; 
    MapGenerator mapGenerator;

    private Dictionary<Vector2, MapCandidate> geoArchive = new Dictionary<Vector2, MapCandidate>();
    private Dictionary<Vector2, MapCandidate> furnArchive = new Dictionary<Vector2, MapCandidate>();
    private Dictionary<Vector2, MapCandidate> enemArchive = new Dictionary<Vector2, MapCandidate>();
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
        RunMapElitesFurnishing();
        RunMapElitesEnemies();
        //RunMapElitesCombined();
        if (enemArchive.Count > 0)
        {
            var keys = enemArchive.Keys.ToList();
            Vector2 bestKey = keys[0];
            foreach (var check in keys)
            {
                if (check.y == 2)
                {
                    bestKey = check;
                    break;
                }
            }

            var best = enemArchive[bestKey];
            Debug.Log("Behavior: " + best.enemyBehavior);
            Debug.Log("Fitness: " + best.fitness);
            mapInstantiator.makeMap(best.mapData.mapArray);
        }

        /*
        if (combinedArchive.Count > 0)
        {
            var best = combinedArchive.Values
                                      .OrderByDescending(c => c.fitness)
                                      .First();

            Debug.Log("Geo:  " + best.geoBehavior);
            Debug.Log("Furn: " + best.furnBehavior);
            Debug.Log("Enemy:" + best.enemyBehavior);
            Debug.Log("Fitness: " + best.fitness);

            mapInstantiator.makeMap(best.mapData.mapArray);
        }*/
    }


    public void RunMapElitesGeometry()
    {
        int iter = 0;

        for (int i = 0; i < totalIterations; i++)
        {
            MapCandidate candidate;

            if (iter <= initialRandomSolutions)
            {
                // x'
                candidate = GenerateRandomGeometry();
                iter++;
            }
            else
            {
                // x
                MapCandidate parent = SelectRandomGeometry();

                // x'
                candidate = MutateGeometry(parent);
            }

            // b'
            candidate.geoBehavior = new Vector2(BehaviorFunctions.GetMapOpennessBehavior(candidate), BehaviorFunctions.GetWindingnessBehavior(candidate));

            // p'
            candidate.fitness = FitnessFunctions.GetGeometryFitness(candidate, (50, 10000, 0.3f), (0.5f, 2f, 0.1f), (1000, 10000, 0.3f), (2, 40, 0.3f));

            // store candidate
            if (!geoArchive.ContainsKey(candidate.geoBehavior) || geoArchive[candidate.geoBehavior].fitness < candidate.fitness)
            {
                geoArchive[candidate.geoBehavior] = candidate;
            }
            //Debug.Log($"Iteration {iter}/{totalIterations} - fitness: {candidate.fitness}, enemies: {behavior.x}, furnishing: {behavior.y}");
        }
    }
 
    public void RunMapElitesFurnishing()
    {
        int iter = 0;

        for (int i = 0; i < totalIterations; i++)
        {
            MapCandidate candidate;

            if (iter <= initialRandomSolutions)
            {
                // x'
                candidate = GenerateRandomFurnishing(SelectRandomGeometry());
                iter++;
            }
            else
            {
                // x
                MapCandidate parent = SelectRandomFurnishing();

                // x'
                candidate = MutateFurnishing(parent);
            }

            // b'
            candidate.furnBehavior = BehaviorFunctions.FurnishingBehaviorPickupDanger(candidate.mapData, BehaviorFunctions.FurnishingBehaviorExploration(candidate.mapData, new Vector2(0, 0)));

            // p'
            candidate.fitness = FitnessFunctions.LootAtEndFitness(candidate.mapData);
            // store candidate
            if (!furnArchive.ContainsKey(candidate.furnBehavior) || furnArchive[candidate.furnBehavior].fitness < candidate.fitness)
            {
                furnArchive[candidate.furnBehavior] = candidate;
            }
            //Debug.Log($"Iteration {iter}/{totalIterations} - fitness: {candidate.fitness}, enemies: {behavior.x}, furnishing: {behavior.y}");
        }

    }

    public void RunMapElitesEnemies()
    {
        int iter = 0;

        for (int i = 0; i < totalIterations; i++)
        {
            MapCandidate candidate;

            if (iter <= initialRandomSolutions)
            {
                // x'
                candidate = GenerateRandomEnemies(SelectRandomFurnishing());
                iter++;
            }
            else
            {
                // x
                MapCandidate parent = SelectRandomEnemies();

                // x'
                candidate = MutateEnemies(parent);
            }

            // b'
            candidate.enemyBehavior = BehaviorFunctions.EnemyClusterBehavior(candidate.mapData, BehaviorFunctions.EnemyCombatMix(candidate.mapData.enemies, new Vector2(0, 0)));

            // p'
            candidate.fitness = FitnessFunctions.EnemyFitnessTotal(candidate.mapData, 0.5f, 0.5f);

            // store candidate
            if (!enemArchive.ContainsKey(candidate.enemyBehavior) || enemArchive[candidate.enemyBehavior].fitness < candidate.fitness)
            {
                enemArchive[candidate.enemyBehavior] = candidate;
            }
            //Debug.Log($"Iteration {iter}/{totalIterations} - fitness: {candidate.fitness}, enemies: {behavior.x}, furnishing: {behavior.y}");
        }
    }

    public void RunMapElitesCombined()
    {
        int iter = 0;

        for (int i = 0; i < totalIterations; i++)
        {
            MapCandidate candidate;

            if (iter <= initialRandomSolutions)
            {
                candidate = GenerateRandomEnemies(GenerateRandomFurnishing(GenerateRandomGeometry()));
                iter++;
            }
            else
            {
                MapCandidate parent = SelectRandomCombined();
                candidate = MutateEnemies(MutateFurnishing(MutateGeometry(parent)));
            }

            candidate.geoBehavior = new Vector2(BehaviorFunctions.GetMapOpennessBehavior(candidate), BehaviorFunctions.GetWindingnessBehavior(candidate));

            candidate.furnBehavior = BehaviorFunctions.FurnishingBehaviorPickupDanger(candidate.mapData, BehaviorFunctions.FurnishingBehaviorExploration(candidate.mapData, Vector2.zero));

            candidate.enemyBehavior = BehaviorFunctions.EnemyClusterBehavior(candidate.mapData, BehaviorFunctions.EnemyCombatMix(candidate.mapData.enemies, Vector2.zero));

            var key = candidate.CombinedBehavior;

            // fitness = sum of sub-fitnesses
            candidate.fitness =
                FitnessFunctions.GetGeometryFitness(candidate, (50, 10000, 0.3f), (0.5f, 2f, 0.1f), (1000, 10000, 0.3f), (2, 40, 0.3f))
              + FitnessFunctions.LootAtEndFitness(candidate.mapData)
              + FitnessFunctions.EnemyFitnessTotal(candidate.mapData, 0.5f, 0.5f);

            if (!combinedArchive.ContainsKey(key) || combinedArchive[key].fitness < candidate.fitness)
            {
                combinedArchive[key] = candidate;
            }
        }
    }


    MapCandidate GenerateRandomGeometry()
    {   
        MapCandidate candidate = new MapCandidate(mapGenerator.MakeMap());
        return candidate;
    }

    MapCandidate GenerateRandomFurnishing(MapCandidate oldCandidate)
    {
        var mapCopy = mapGenerator.placeFurnishing(oldCandidate.mapData.Clone());
        return new MapCandidate(mapCopy);
    }

    MapCandidate GenerateRandomEnemies(MapCandidate oldCandidate)
    {
        var mapCopy = mapGenerator.placeEnemies(oldCandidate.mapData.Clone());
        return new MapCandidate(mapCopy);
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

    MapCandidate MutateFurnishing(MapCandidate parent)
    {
        var child = new MapCandidate(parent.mapData.Clone());
        child.mapData = mapGenerator.mutateFurnishing(child.mapData);
        return child;
    }
    
     MapCandidate MutateEnemies(MapCandidate parent)
    {
        var child = new MapCandidate(parent.mapData.Clone());
        child.mapData = mapGenerator.mutateEnemies(child.mapData);
        return child;
    }
}

public class MapCandidate
{
    public float fitness;
    public MapInfo mapData;

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
        fitness = 0f;
        geoBehavior = new Vector2() ;
        furnBehavior = new Vector2();
        enemyBehavior = new Vector2();
    }
}



