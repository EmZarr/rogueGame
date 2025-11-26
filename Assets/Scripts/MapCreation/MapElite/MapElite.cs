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
    //private List<MapCandidate> archive = new List<MapCandidate>();

    private Dictionary<Vector2, MapCandidate> archive = new Dictionary<Vector2, MapCandidate>();

    private int iter;
    private void Awake()
    {
        //Debug.unityLogger.logEnabled = false;
    }
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
        //Debug.Log("Archive size start: " + archive.Count());
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
                candidate = new MapCandidate(parent.mapData.Clone());
                candidate = MutateGeometry(candidate);
            }

            // b'
            //candidate.behavior = new Vector2(BehaviorFunctions.GetMapOpennessBehavior(candidate), BehaviorFunctions.GetWindingnessBehavior(candidate));
            candidate.behavior = BehaviorFunctions.EnemyClusterBehavior(candidate.mapData, BehaviorFunctions.EnemyCombatMix(candidate.mapData.enemies, new Vector2(0, 0)));

            // p'
            //candidate.fitness = FitnessFunctions.GetGeometryFitness(candidate, (50, 10000, 0.3f), (0.5f, 2f, 0.1f), (1000, 10000, 0.3f), (2, 40, 0.3f));
            candidate.fitness = FitnessFunctions.EnemyFitnessTotal(candidate.mapData, 0.5f, 0.5f);

            // store candidate
            if (!archive.ContainsKey(candidate.behavior) || archive[candidate.behavior].fitness < candidate.fitness)
            {
                InsertIntoArchive(candidate, candidate.behavior);
            }
            //Debug.Log($"Iteration {iter}/{totalIterations} - fitness: {candidate.fitness}, enemies: {behavior.x}, furnishing: {behavior.y}");
        }
        if (archive.Count > 0)
        {


            var keys = archive.Keys.ToList();
            Vector2 bestKey = keys[0];
            foreach (var check in keys)
            {
                if (check.y == 2)
                {
                    bestKey = check;
                    break;
                }
            }

            var best = archive[bestKey];
            Debug.Log("Behavior: " + best.behavior);
            Debug.Log("Fitness: " + best.fitness);
            int pathCount = best.mapData.shortestPath?.Count ?? 0;
            Debug.Log("Path count " + pathCount);
            mapInstantiator.makeMap(best.mapData.mapArray);
        }

        //ExportArchiveToJson();
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
        int val = candidate.mapData.components.Count();
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
        Debug.Log("Fitness: " +  FitnessFunctions.RoomFitnessTotal(candidate.mapData));
        return candidate.mapData.floorTiles.Count * 0.1f + FitnessFunctions.RoomFitnessTotal(candidate.mapData);
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

    // Export json here baby ahahahs
    public void ExportArchiveToJson(string filename = "archive_maps.json")
    {
        var collection = new MapCollection();
        collection.maps = new List<MapDTO>();

        foreach (var kv in archive)
        {
            Vector2 behavior = kv.Key;
            MapCandidate candidate = kv.Value;
            var map = candidate.mapData;

            int width = map.mapArray?.GetLength(0) ?? 0;
            int height = map.mapArray?.GetLength(1) ?? 0;

            MapDTO dto = new MapDTO
            {
                width = width,
                height = height,
                tiles = map.mapArray != null ? ConvertMapArrayToNestedList(map.mapArray) : new List<List<int>>(),
                flatTiles = map.mapArray != null ? FlattenMapArray(map.mapArray) : new List<int>(),
                fitness = candidate.fitness,
                behavior = ConvertBehaviorToList(behavior),

                // Summary metrics
                roomsCount = map.components?.Count ?? 0,
                enemiesCount = map.enemies?.Count ?? 0,
                furnishingCount = map.furnishing?.Count ?? 0,
                budget = map.enemiesBudget,
                furnishingBudget = map.furnishingBudget,
                walkableTiles = map.floorTiles.Count,
            };

            collection.maps.Add(dto);
        }

        string json = JsonUtility.ToJson(collection, true);
        string path = Path.Combine(Application.dataPath, filename);
        File.WriteAllText(path, json);
        Debug.Log($"Archive exported to {path} ({collection.maps.Count} maps)");
    }

    List<int> FlattenMapArray(int[,] mapArray)
    {
        int width = mapArray.GetLength(0);
        int height = mapArray.GetLength(1);

        var flat = new List<int>(width * height);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                flat.Add(mapArray[x, y]);
            }
        }
        return flat;
    }

    List<List<int>> ConvertMapArrayToNestedList(int[,] mapArray)
    {
        int width = mapArray.GetLength(0);
        int height = mapArray.GetLength(1);

        var rows = new List<List<int>>(height);
        for (int y = 0; y < height; y++)
        {
            var row = new List<int>(width);
            for (int x = 0; x < width; x++)
            {
                row.Add(mapArray[x, y]);
            }
            rows.Add(row);
        }
        return rows;
    }

    List<float> ConvertBehaviorToList(Vector2 behavior)
    {
        return new List<float> { behavior.x, behavior.y };
    }

    [System.Serializable]
    public class MapDTO
    {
        public int width;
        public int height;
        public List<List<int>> tiles; // tiles[y][x]

        // flattened row-major list: length == width * height
        public List<int> flatTiles;

        public float fitness;
        public List<float> behavior; // scalable behavior vector components

        // Summary metrics
        public int roomsCount;
        public int enemiesCount;
        public int furnishingCount;
        public int budget;
        public int furnishingBudget;
        public int walkableTiles;
        public int wallTiles;
    }

    [System.Serializable]
    public class MapCollection
    {
        public List<MapDTO> maps;
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


