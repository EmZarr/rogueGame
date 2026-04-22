using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.IO;


public class MapSelector : MonoBehaviour
{

    void Start(){
        string path = Path.Combine(Application.streamingAssetsPath, "enemArchive_maps.json");
        var maps = MapJsonExporter.LoadMaps(path);
        Debug.Log("hello?: " + maps.Count);
        SelectMaps(maps);
    }
    void SelectMaps(List<Map> maps)
    {
        List<Map> selectedMaps = new List<Map>();
        var diverseEnemyBehaviorMaps = SelectMostDiverseEnemyBehavior(maps, 4);
        var diverseExplorationBehaviorMaps = SelectMostDiverseExplorationBehavior(maps, 4);
        Debug.Log("Enemy Maps final: " + diverseEnemyBehaviorMaps.Count);
        Debug.Log("Explore Maps final: " + diverseExplorationBehaviorMaps.Count);

        foreach (var map in diverseEnemyBehaviorMaps)
        {
            selectedMaps.Add(map);
        }
        foreach (var map in diverseExplorationBehaviorMaps)
        {
            selectedMaps.Add(map);
        }
        MapJsonExporter.SaveMaps(selectedMaps, "Diverse_Maps.json");

    }


    //Finds the maps that have the most diverse enemy composition comparetively to each other and the total average
    List<Map> SelectMostDiverseEnemyBehavior(List<Map> maps, int amountToSelect)
    {
        float[] averageEnemyComposition = new float[5];
        List<Map> selectedMaps = new List<Map>();
        
        //Calculate average enemy composition for all maps
        foreach (var map in maps)
        {
            var mapEnemyComposition = EnemFitAndBehav.GetEnemyComposition(map.GetAllEnemies());
            for(int i=0; i<5; i++)
            {
                averageEnemyComposition[i] += mapEnemyComposition[i];
            }
        }
        for (int i=0; i<5; i++)
        {
            averageEnemyComposition[i] /= (float) maps.Count;
            Debug.Log("avg EneComp" + i + ": " + averageEnemyComposition[i]);
        }
        //Add level with closest to average enemy composition
        float highestSimilarity = 0f;
        Map mapToAdd = new Map();
        foreach (var map in maps)
        {
            float similarity = EnemFitAndBehav.GetCompositionSimilarity(averageEnemyComposition, EnemFitAndBehav.GetEnemyComposition(map.GetAllEnemies()));
            if(similarity > highestSimilarity)
                {
                    highestSimilarity = similarity;
                    mapToAdd = map;
                }
        }
        selectedMaps.Add(mapToAdd);

        //Add n (specified) amount of maps, decided by difference in enemy composition to already added maps.
        float lowestSimilarity;
        while (selectedMaps.Count < amountToSelect){
            lowestSimilarity = (float) selectedMaps.Count;
            foreach (var map in maps)
            {
                float similarity = 0;
                //Ensures that maps are different from each other all together and not just multiple maps in two extreme ends of the enemy composition scale
                foreach(var selectedMap in selectedMaps){
                    similarity += EnemFitAndBehav.GetCompositionSimilarity(EnemFitAndBehav.GetEnemyComposition(selectedMap.GetAllEnemies()), EnemFitAndBehav.GetEnemyComposition(map.GetAllEnemies()));
                }
                if(similarity < lowestSimilarity)
                        {
                            lowestSimilarity = similarity;
                            mapToAdd = map;
                        }
            }
            selectedMaps.Add(mapToAdd);
        }
        return selectedMaps;
    }

    //Finds the maps that has the most diverse exploration (geometry + furnishing) behavior comparetively to each other and the global average
    List<Map> SelectMostDiverseExplorationBehavior(List<Map> maps, int amountToSelect)
    {
        //Finds the average exploration behavior for all maps
        float[] averageExplorationBehavior = new float[4];
        List<Map> selectedMaps = new List<Map>();
        foreach (var map in maps)
        {
            float[] mapExploreComposition = GetMapExplorationBehavior(map);
            for(int i=0; i<4; i++)
            {
                averageExplorationBehavior[i] += mapExploreComposition[i];
            }
        }
        for (int i=0; i<4; i++)
        {
            averageExplorationBehavior[i] /= (float) maps.Count;
            Debug.Log("avg ExploreBehave" + i + ": " + averageExplorationBehavior[i]);
        }
        //Add level with closest to average exploration behavior
        float highestSimilarity = 0f;
        Map mapToAdd = new Map();
        foreach (var map in maps)
        {
            float similarity = EnemFitAndBehav.GetCompositionSimilarity(averageExplorationBehavior, GetMapExplorationBehavior(map));
            if(similarity > highestSimilarity)
                {
                    highestSimilarity = similarity;
                    mapToAdd = map;
                }
        }
        selectedMaps.Add(mapToAdd);

        //Add n (specified) amount of levels based on their difference in exploration behavior, compared to previously added maps.
        float lowestSimilarity;
        while (selectedMaps.Count < amountToSelect){
            lowestSimilarity = (float) selectedMaps.Count;
            foreach (var map in maps)
            {
                float similarity = 0;
                foreach(var selectedMap in selectedMaps){
                    similarity += EnemFitAndBehav.GetCompositionSimilarity(GetMapExplorationBehavior(selectedMap), GetMapExplorationBehavior(map));
                }
                if(similarity < lowestSimilarity)
                        {
                            lowestSimilarity = similarity;
                            mapToAdd = map;
                        }
            }
            selectedMaps.Add(mapToAdd);
        }
        return selectedMaps;
    }



    //Helper function to translate geometry + furnishing behavior into a exploration behavior array
    float[] GetMapExplorationBehavior(Map map)
    {
        return new float[] {map.geoBehavior.x, map.geoBehavior.y, map.furnBehavior.x, map.furnBehavior.y};
    }

}