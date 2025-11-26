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
    private Dictionary<Vector2, MapCandidate> EnemArchive = new Dictionary<Vector2, MapCandidate>();

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
                MapCandidate parent = SelectRandomParent();

                // x'
                candidate = MutateGeometry(parent);
            }

            // b'
            //candidate.behavior = new Vector2(BehaviorFunctions.GetMapOpennessBehavior(candidate), BehaviorFunctions.GetWindingnessBehavior(candidate));
            candidate.behavior = BehaviorFunctions.EnemyClusterBehavior(candidate.mapData, BehaviorFunctions.EnemyCombatMix(candidate.mapData.enemies, new Vector2(0, 0)));

            // p'
            //candidate.fitness = FitnessFunctions.GetGeometryFitness(candidate, (50, 10000, 0.3f), (0.5f, 2f, 0.1f), (1000, 10000, 0.3f), (2, 40, 0.3f));
            candidate.fitness = FitnessFunctions.EnemyFitnessTotal(candidate.mapData, 0.5f, 0.5f);

            // store candidate
            if (!geoArchive.ContainsKey(candidate.behavior) || geoArchive[candidate.behavior].fitness < candidate.fitness)
            {
                InsertIntoArchive(candidate, candidate.behavior);
            }
            //Debug.Log($"Iteration {iter}/{totalIterations} - fitness: {candidate.fitness}, enemies: {behavior.x}, furnishing: {behavior.y}");
        }
        if (geoArchive.Count > 0)
        {


            var keys = geoArchive.Keys.ToList();
            Vector2 bestKey = keys[0];
            foreach (var check in keys)
            {
                if (check.y == 2)
                {
                    bestKey = check;
                    break;
                }
            }

            var best = geoArchive[bestKey];
            Debug.Log("Behavior: " + best.behavior);
            Debug.Log("Fitness: " + best.fitness);
            int pathCount = best.mapData.shortestPath?.Count ?? 0;
            Debug.Log("Path count " + pathCount);
            mapInstantiator.makeMap(best.mapData.mapArray);
        }
    }
    /*
    public void RunMapElitesFurnishing()
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
                MapCandidate parent = SelectRandomParent();

                // x'
                candidate = MutateGeometry(parent);
            }

            // b'
            candidate.behavior = new Vector2(BehaviorFunctions.GetMapOpennessBehavior(candidate), BehaviorFunctions.GetWindingnessBehavior(candidate));

            // p'
            candidate.fitness = FitnessFunctions.GetGeometryFitness(candidate, (50, 10000, 0.3f), (0.5f, 2f, 0.1f), (1000, 10000, 0.3f), (2, 40, 0.3f));

            // store candidate
            if (!geoArchive.ContainsKey(candidate.behavior) || geoArchive[candidate.behavior].fitness < candidate.fitness)
            {
                InsertIntoArchive(candidate, candidate.behavior);
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
                candidate = GenerateRandomGeometry();
                iter++;
            }
            else
            {
                // x
                MapCandidate parent = SelectRandomParent();

                // x'
                candidate = MutateGeometry(parent);
            }

            // b'
            candidate.behavior = new Vector2(BehaviorFunctions.GetMapOpennessBehavior(candidate), BehaviorFunctions.GetWindingnessBehavior(candidate));

            // p'
            candidate.fitness = FitnessFunctions.GetGeometryFitness(candidate, (50, 10000, 0.3f), (0.5f, 2f, 0.1f), (1000, 10000, 0.3f), (2, 40, 0.3f));

            // store candidate
            if (!geoArchive.ContainsKey(candidate.behavior) || geoArchive[candidate.behavior].fitness < candidate.fitness)
            {
                InsertIntoArchive(candidate, candidate.behavior);
            }
            //Debug.Log($"Iteration {iter}/{totalIterations} - fitness: {candidate.fitness}, enemies: {behavior.x}, furnishing: {behavior.y}");
        }
    }*/

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
    MapCandidate SelectRandomParent()
    {
        //Return random solution in the archive
        return archive.Values.ToList()[Random.Range(0, archive.Count)];
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

    void InsertIntoArchive(MapCandidate candidate, Vector2 behavior)
    {
        // behavior here
        if (!archive.ContainsKey(behavior))
        {
            archive.Add(behavior, candidate);
        }
        else
        {
            archive[behavior] = candidate;
        }

    }
}

public class MapCandidate
{
    //public int[,] mapData;
    public float fitness;

    public Vector2 behavior;

    public MapInfo mapData;

    public MapCandidate(MapInfo map)
    {
        mapData = map;
        fitness = 0f;
        behavior = new Vector2();
    }
}


