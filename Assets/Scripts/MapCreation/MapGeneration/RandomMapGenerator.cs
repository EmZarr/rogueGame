using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using System.Text;

public class RandomMapGenerator : MonoBehaviour
{

    [SerializeField]
    int RandomMapAmount = 50;

    private List<Map> geoArchive = new List<Map>();
    private List<Map> furnArchive = new List<Map>();
    private List<Map> enemArchive = new List<Map>();
    void Start()
    {
        GenerateRandomMaps(RandomMapAmount);
    }

    //uses same hiarchical apporach as MAP-elites, but just creates n amount of maps randomly for each layer, 
    //with no mutations and saving them all, regardless of behavior duplicates.
    void GenerateRandomMaps(int amount)
    {
        for(int i = 0; i<amount; i++)
        {
            var newGeoMap = new Map();
            GeometryGenerator.CreateMapGeometry(newGeoMap);
            GeometryGenerator.BuildRoomTopology(newGeoMap);
            var (fitness, behavior) = GeoFitAndBehav.GetGeoFitnessAndBehavior(newGeoMap);
            newGeoMap.geoBehavior = new Vector2Int(behavior, 0);
            newGeoMap.geoFitness = fitness;
            geoArchive.Add(newGeoMap);
        }

        for(int i = 0; i<amount; i++)
        {
            Map selectedGeoMap = geoArchive[i];
            var newFurnMap  = new Map(selectedGeoMap.Clone());
            ObjectPlacementGenerator.CreateLootOnMap(newFurnMap);
            ObjectPlacementGenerator.CreateObstaclesOnMap(newFurnMap);
            var (fitness, behavior) = FurnFitAndBehav.GetFurnFitnessAndBehavior(newFurnMap);
            newFurnMap.geoBehavior = selectedGeoMap.geoBehavior;
            newFurnMap.geoFitness = selectedGeoMap.geoFitness;
            newFurnMap.furnBehavior = new Vector2Int(behavior.lootDensity, behavior.obstacleDensity);
            newFurnMap.furnFitness = fitness;
            furnArchive.Add(newFurnMap);
        }

        for(int i = 0; i<amount; i++)
        {
            Map selectedFurnMap = furnArchive[i];
            var newEneMap = new Map(selectedFurnMap.Clone());
            ObjectPlacementGenerator.CreateEnemiesOnMap(newEneMap);
            newEneMap.geoBehavior = selectedFurnMap.geoBehavior;
            newEneMap.geoFitness = selectedFurnMap.geoFitness;
            newEneMap.furnBehavior = selectedFurnMap.furnBehavior;
            newEneMap.furnFitness = selectedFurnMap.furnFitness;
            var (fitness, behavior) = EnemFitAndBehav.GetEnemyFitnessAndBehavior(newEneMap);
            newEneMap.enemyBehavior = new Vector2Int(behavior.enemyType, behavior.difficulty);
            newEneMap.enemFitness = fitness;
            Debug.Log("Geo: " + newEneMap.geoBehavior);
            Debug.Log("Furn: " + newEneMap.furnBehavior);
            Debug.Log("Ene: " + newEneMap.enemyBehavior);
            enemArchive.Add(newEneMap);
        }
        MapJsonExporter.SaveMaps(enemArchive, "Random_Maps.json");
    }



}
