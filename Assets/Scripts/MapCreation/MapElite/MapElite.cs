using System.Collections.Generic;
using UnityEngine;
using System.Linq;
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
    //private List<MapCandidate> archive = new List<MapCandidate>();

    private Dictionary<Vector2, MapCandidate> archive = new Dictionary<Vector2, MapCandidate>();

    private int iter;

    void Start()
    {
        runElites.action.Enable();
        runElites.action.performed += ctx => RunMapElites();
        mapGenerator = GetComponent<MapGenerator>();
        iter = 0;
        RunMapElites();
    }


    public void RunMapElites()
    {
        Debug.Log("Archive size start: " + archive.Count());
        //archive.Clear();
        for (int i = 0; i < totalIterations; i++)
        {
            MapCandidate candidate;

            if (iter <= initialRandomSolutions)
            {
                // x'
                candidate = GenerateRandomCandidate();
                iter++;
            }
            else
            {
                // x
                MapCandidate parent = SelectParent();

                // x'
                candidate = new MapCandidate(MutateGeometry(parent).mapData);
            }

            // b'
            Vector2 behavior = GeometryBehavior(candidate);

            // p'
            candidate.fitness = EvaluateFitnessFunction(candidate, behavior);

            // store candidate
            if (!archive.ContainsKey(behavior) || archive[behavior].fitness < candidate.fitness)
            {
                InsertIntoArchive(candidate, behavior);
            }
            //Debug.Log($"Iteration {iter}/{totalIterations} - fitness: {candidate.fitness}, enemies: {behavior.x}, furnishing: {behavior.y}");
        }
        if (archive.Count > 0)
        {


            var vals = archive.Values.ToList();
            MapCandidate best = vals[0];
            foreach (var check in vals)
            {
                if (check.fitness > best.fitness)
                {
                    best = check;
                }
            }

            Debug.Log("Best fitness: " + best.fitness);
            //Debug.Log("Spawning best map via original MapInstantiator.");
            mapInstantiator.makeMap(best.mapData.mapArray);
        }
    }

    MapCandidate GenerateRandomCandidate()
    {
        // Instantiate a temporary generator
        mapGenerator.RemakeMap();
        MapCandidate candidate = new MapCandidate(mapGenerator.currentMap);
        return candidate;
    }

    MapCandidate SelectParent()
    {
        if (archive.Count == 0)
            return GenerateRandomCandidate(); //this may not be necessary

        //Return random solution in the archive
        return archive.Values.ToList()[Random.Range(0, archive.Count)];
    }

    Vector2 FeatureDescriptorFunction(MapCandidate candidate)
    {
        int enemyCount = 0;
        int furnishingCount = 0;

        int width = candidate.mapData.mapArray.GetLength(0);
        int height = candidate.mapData.mapArray.GetLength(1);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int val = candidate.mapData.mapArray[x, y];

                // according to MapInstantiator:
                // 3–5 = furn, 6–7 hahahah
                if (val >= 6 && val <= 7)
                    enemyCount++;
                else if (val >= 3 && val <= 5)
                    furnishingCount++;
            }
        }

        return new Vector2(enemyCount, furnishingCount);
    }


    Vector2 GeometryBehavior(MapCandidate candidate)
    {
        int val = candidate.mapData.rooms.Count();
        int key;
        if (val <= 3)
        {
            key = 1;
        }
        else if (val > 3 && val <= 6)
        {
            key = 2;
        }
        else if (val > 6 && val <= 9)
        {
            key = 3;
        }
        else if (val > 9 && val <= 12)
        {
            key = 4;
        }
        else
        {
            key = 5;
        }
        return new Vector2(key, 0);
    }

    MapCandidate MutateGeometry(MapCandidate parent)
    {
        parent.mapData = mapGenerator.MutateMap(parent.mapData);
        return parent;
    }

    MapCandidate MutateFurnishing(MapCandidate parent)
    {
        parent.mapData = mapGenerator.MutateContent(parent.mapData);
        return parent;
    }
    
     MapCandidate MutateEnemies(MapCandidate parent)
    {
        parent.mapData = mapGenerator.MutatePlacements(parent.mapData);
        return parent;
    }

    float EvaluateFitnessFunction(MapCandidate candidate, Vector2 behavior)
    {
        return candidate.mapData.walkableTiles * 0.1f;
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
    
    public List<int[,]> mapArrayList()
    {
        List<int[,]> list = new List<int[,]>();
        foreach (var map in archive.Values.ToList())
        {
            list.Add(map.mapData.mapArray);
        }
        return list;
    }
}

public class MapCandidate
{
    //public int[,] mapData;
    public float fitness;

    public MapInfo mapData;

    public MapCandidate(MapInfo map)
    {
        mapData = map;
        fitness = 0f;
    }
}

public struct Behavior
{
    int temp;
}

