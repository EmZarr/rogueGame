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
        float avgGeoFit = 0;
        float averageTotalFitness = 0;
        float counter = 0;
        string path = Path.Combine(Application.dataPath, "GeoFitness.csv");
        var sb = new StringBuilder();
        sb.AppendLine("iterations, geometry fitness, total fitness");

        for (int i = 0; i < totalIterations; i++)
        {
            MapCandidate candidate;
            if (i%1000 == 0)
            {
                avgGeoFit = avgGeoFit/counter;
                averageTotalFitness /= counter;


                sb.AppendLine(i.ToString()+ ", " + avgGeoFit.ToString()+ ", " + averageTotalFitness.ToString());
                avgGeoFit = 0;
                averageTotalFitness = 0;
                counter=0;
            }

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
            candidate.geoBehavior = new Vector2(BehaviorFunctions.GetMapOpennessBehavior(candidate, 10), BehaviorFunctions.GetWindingnessBehavior(candidate, 10));

            // p'
            candidate.geoFitness = FitnessFunctions.GetGeometryFitness(candidate, (50, 10000, 0.35f), (0.5f, 2f, 0.1f), (1000, 3000, 0.35f), (2, 40, 0.1f), (0f, 0.15f, 0.1f));

            // store candidate
            if (!geoArchive.ContainsKey(candidate.geoBehavior) || geoArchive[candidate.geoBehavior].CombinedFitness < candidate.CombinedFitness)
            {
                geoArchive[candidate.geoBehavior] = candidate;
                if (!float.IsNaN(candidate.geoFitness))
                {
                    avgGeoFit += candidate.geoFitness;
                    averageTotalFitness += candidate.CombinedFitness;
                    counter++;
                }
                
            }
            //Debug.Log($"Iteration {iter}/{totalIterations} - fitness: {candidate.fitness}, enemies: {behavior.x}, furnishing: {behavior.y}");
        }
        File.WriteAllText(path, sb.ToString());
    }

    public void RunMapElitesEnemies()
    {
        int iter = 0;
        float avgEneFit = 0;
        float averageTotalFitness = 0;
        float counter = 0;
        string path = Path.Combine(Application.dataPath, "EneFitness.csv");
        var sb = new StringBuilder();
        sb.AppendLine("iterations, enemy fitness, total fitness");

        for (int i = 0; i < totalIterations; i++)
        {
            MapCandidate candidate;
            if (i%1000 == 0)
            {
                avgEneFit = avgEneFit/counter;
                averageTotalFitness /= counter;


                sb.AppendLine(i.ToString()+ ", " + avgEneFit.ToString() + ", " + averageTotalFitness.ToString());
                avgEneFit = 0;
                averageTotalFitness = 0;
                counter=0;
            }

            if (iter <= initialRandomSolutions)
            {
                // x'
                candidate = GenerateRandomEnemies(SelectRandomGeometry());
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
            candidate.enemFitness = FitnessFunctions.EnemyFitnessTotal(candidate.mapData, 0.5f, 0.5f);

            // store candidate
            if (!enemArchive.ContainsKey((candidate.geoBehavior, candidate.enemyBehavior)) || enemArchive[(candidate.geoBehavior, candidate.enemyBehavior)].CombinedFitness < candidate.CombinedFitness)
            {
                enemArchive[(candidate.geoBehavior, candidate.enemyBehavior)] = candidate;
                if (!float.IsNaN(candidate.enemFitness))
                {
                    avgEneFit += candidate.enemFitness;
                    averageTotalFitness += candidate.CombinedFitness;
                    counter++;
                }
            }
            //Debug.Log($"Iteration {iter}/{totalIterations} - fitness: {candidate.fitness}, enemies: {behavior.x}, furnishing: {behavior.y}");
        }
        File.WriteAllText(path, sb.ToString());
    }

    public void RunMapElitesFurnishing()
    {
        int iter = 0;
        float avgFurFit = 0;
        float averageTotalFitness = 0;
        float counter = 0;
        string path = Path.Combine(Application.dataPath, "FurFitness.csv");
        var sb = new StringBuilder();
        sb.AppendLine("iterations, furnish fitness, total fitness");

        for (int i = 0; i < totalIterations; i++)
        {
            MapCandidate candidate;
            if (i%1000 == 0)
            {
                avgFurFit = avgFurFit/counter;
                averageTotalFitness /= counter;

                sb.AppendLine(i.ToString()+ ", " + avgFurFit.ToString() + ", " + averageTotalFitness.ToString());
                avgFurFit = 0;
                averageTotalFitness = 0;
                counter=0;
            }

            if (iter <= initialRandomSolutions)
            {
                // x'
                candidate = GenerateRandomFurnishing(SelectRandomEnemies());
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
            candidate.furnFitness = FitnessFunctions.LootAtEndFitness(candidate.mapData);
            // store candidate
            if (!furnArchive.ContainsKey((candidate.geoBehavior, candidate.enemyBehavior, candidate.furnBehavior)) || furnArchive[(candidate.geoBehavior, candidate.enemyBehavior, candidate.furnBehavior)].CombinedFitness < candidate.CombinedFitness)
            {
                furnArchive[(candidate.geoBehavior, candidate.enemyBehavior, candidate.furnBehavior)] = candidate;
                if (!float.IsNaN(candidate.furnFitness))
                {
                    avgFurFit += candidate.furnFitness;
                    averageTotalFitness += candidate.CombinedFitness;
                    counter++;
                }
            }
            //Debug.Log($"Iteration {iter}/{totalIterations} - fitness: {candidate.fitness}, enemies: {behavior.x}, furnishing: {behavior.y}");
        }
        File.WriteAllText(path, sb.ToString());

    }

    public void RunMapElitesCombined()
    {
        int iter = 0;
        float averageFitness = 0;
        float avgGeoFit = 0;
        float avgEneFit = 0;
        float avgFurFit = 0;
        float counter = 0;
        string path = Path.Combine(Application.dataPath, "fitness.csv");
        var sb = new StringBuilder();
        sb.AppendLine("iterations, total fitness score, geometry fitness, furnishing fitness, enemy fitness");
        for (int i = 0; i < totalIterations; i++)
        {
            MapCandidate candidate;
            if (i%1000 == 0)
            {
                float avgFit = averageFitness/counter;
                avgGeoFit = avgGeoFit/counter;
                avgEneFit = avgEneFit/counter;
                avgFurFit = avgFurFit/counter;

                sb.AppendLine(i.ToString()+ ", " + avgFit.ToString()+ ", "+avgGeoFit.ToString()+ ", " + avgFurFit.ToString() + ", " + avgEneFit.ToString());
                averageFitness = 0;
                avgGeoFit = 0;
                avgFurFit = 0;
                avgEneFit = 0;
                counter=0;
            }

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

            candidate.geoBehavior = new Vector2(BehaviorFunctions.GetMapOpennessBehavior(candidate, 5), BehaviorFunctions.GetWindingnessBehavior(candidate, 5));

            candidate.furnBehavior = BehaviorFunctions.FurnishingBehaviorPickupDanger(candidate.mapData, BehaviorFunctions.FurnishingBehaviorExploration(candidate.mapData, Vector2.zero));

            candidate.enemyBehavior = BehaviorFunctions.EnemyClusterBehavior(candidate.mapData, BehaviorFunctions.EnemyCombatMix(candidate.mapData.enemies, Vector2.zero));

            var key = candidate.CombinedBehavior;

            candidate.geoFitness = FitnessFunctions.GetGeometryFitness(candidate, (50, 10000, 0.35f), (0.5f, 2f, 0.1f), (1000, 3000, 0.35f), (2, 40, 0.1f), (0f, 0.15f, 0.1f));
            candidate.furnFitness = FitnessFunctions.FurnishingFitnessTotal(candidate.mapData, 0.34f, 0.33f, 0.33f);
            candidate.enemFitness = FitnessFunctions.EnemyFitnessTotal(candidate.mapData, 0.5f, 0.5f);
            
            
            if (!combinedArchive.ContainsKey(key) || combinedArchive[key].CombinedFitness < candidate.CombinedFitness)
            {
                combinedArchive[key] = candidate;
                if (!float.IsNaN(candidate.geoFitness))
                {
                    avgGeoFit += candidate.geoFitness;
                }
                if (!float.IsNaN(candidate.furnFitness))
                {
                    avgFurFit += candidate.furnFitness;
                }
                if (!float.IsNaN(candidate.enemFitness))
                {
                    avgEneFit += candidate.enemFitness;
                }
                
                if (!float.IsNaN(candidate.CombinedFitness))
                {
                    averageFitness += candidate.CombinedFitness;
                }
                counter++;
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



