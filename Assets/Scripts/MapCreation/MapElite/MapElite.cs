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
        //archive.Clear();
        Debug.Log("Test");
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
                candidate = MutateFunction(parent);
                Debug.Log("here");
            }

            // b'
            Vector2 behavior = FeatureDescriptorFunction(candidate);

            // p'
            candidate.fitness = EvaluateFitnessFunction(candidate, behavior);

            // store candidate
            if (!archive.ContainsKey(behavior) || archive[behavior].fitness < candidate.fitness)
            {
                InsertIntoArchive(candidate, behavior);
            }
            Debug.Log($"Iteration {iter}/{totalIterations} - fitness: {candidate.fitness}, enemies: {behavior.x}, furnishing: {behavior.y}");
        }

        if (archive.Count > 0)
        {
            var vals = archive.Values.ToList();
            MapCandidate best = vals[0];
            foreach (var candidate in vals)
            {
                if (candidate.fitness > best.fitness) {
                    best = candidate;
                }
            }

            Debug.Log("Spawning best map via original MapInstantiator.");
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

    MapCandidate MutateFunction(MapCandidate parent)
    {
        parent.mapData = mapGenerator.MutateMap(parent.mapData);
        parent.mapData = mapGenerator.MutateContent(parent.mapData);
        parent.mapData = mapGenerator.MutatePlacements(parent.mapData);
       
        //Mutate map layout here
        // Mutate enemies here
        // Mutate furnishing here

        return parent;
        // For now, just create a new random candidate as a placeholder
        //return GenerateRandomCandidate();
    }

    float EvaluateFitnessFunction(MapCandidate candidate, Vector2 behavior)
    {
        // placeholder
        float enemyCount = behavior.x;
        float furnishingCount = behavior.y;
        return enemyCount + furnishingCount * 0.25f;
    }

    void InsertIntoArchive(MapCandidate candidate, Vector2 behavior)
    {
        // behavior here
        archive.Add(behavior, candidate);
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

/*public struct MapInfo
    {
        public int[,] mapArray;
        public List<(Vector3Int placement, Vector3Int size)> rooms;
        public List<(Vector2Int placement, int type)> enemies;
        public Vector2Int playerStartPos;
        public Vector3Int outlinePlacement;
        public Vector3Int outlineSize;
        public List<(Vector2Int start, Vector2Int end)> componentConnections;
        public int budget;
        public List<(Vector2Int placement, int type)> furnishing;
        public int furnishingBudget;
    }
    */
