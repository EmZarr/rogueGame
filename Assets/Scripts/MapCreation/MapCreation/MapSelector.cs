using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.IO;


public class MapSelector : MonoBehaviour
{
    [SerializeField]
    SelectorLogger logger;

    [SerializeField]

    string archiveTostoreIn;

    void Start(){
        string path = Path.Combine(Application.streamingAssetsPath, "enemArchiveEasy_maps.json");
        var maps = MapJsonExporter.LoadMaps(path);
        SelectMaps(maps);
    }
    void SelectMaps(List<Map> maps)
    {
        List<Map> selectedMaps = new List<Map>();
        var diverseEnemyBehaviorMaps = SelectMostDiverseEnemyBehavior(maps, 3);
        var diverseExplorationBehaviorMaps = SelectMostDiverseExplorationBehavior(maps, 3);
        //Debug.Log("Enemy Maps final: " + diverseEnemyBehaviorMaps.Count);
        //Debug.Log("Explore Maps final: " + diverseExplorationBehaviorMaps.Count);

        foreach (var map in diverseEnemyBehaviorMaps)
        {
            selectedMaps.Add(map);
        }
        foreach (var map in diverseExplorationBehaviorMaps)
        {
            selectedMaps.Add(map);
        }
        MapJsonExporter.SaveMaps(selectedMaps, archiveTostoreIn);

    }


    //Finds the maps that have the most diverse enemy composition comparetively to each other and the total average
    List<Map> SelectMostDiverseEnemyBehavior(List<Map> maps, int amountToSelect)
    {
        float[] averageEnemyComposition = new float[5];
        List<Map> selectedMaps = new List<Map>();
        HashSet<Vector2> chosenBehaviors = new HashSet<Vector2>();
        
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
        float enemyCount = 0;
        foreach (var map in maps)
        {

            //Brute force fix, no good, but best option
            float similarity = EnemFitAndBehav.GetCompositionSimilarity(averageEnemyComposition, EnemFitAndBehav.GetEnemyComposition(map.GetAllEnemies()));
            if(similarity > highestSimilarity)
            {
                highestSimilarity = similarity;
                mapToAdd = map;
            }
        }
        selectedMaps.Add(mapToAdd);
        chosenBehaviors.Add(mapToAdd.enemyBehavior);
        logger.LogEnemyComp(mapToAdd, EnemFitAndBehav.GetEnemyComposition(mapToAdd.GetAllEnemies()), 1-highestSimilarity);

        //Add n (specified) amount of maps, decided by difference in enemy composition to already added maps.
        float lowestSimilarity;
        while (selectedMaps.Count < amountToSelect){
            lowestSimilarity = (float) selectedMaps.Count;
            foreach (var map in maps)
            {
                if (chosenBehaviors.Contains(map.enemyBehavior))
                {
                    continue;
                }
                float similarity = 0;
                //Ensures that maps are different from each other all together and not just multiple maps in two extreme ends of the enemy composition scale
                foreach(var selectedMap in selectedMaps){
                    similarity += EnemFitAndBehav.GetCompositionSimilarity(EnemFitAndBehav.GetEnemyComposition(selectedMap.GetAllEnemies()), EnemFitAndBehav.GetEnemyComposition(map.GetAllEnemies()));
                }
                similarity /= selectedMaps.Count;
                if(similarity < lowestSimilarity)
                        {
                            lowestSimilarity = similarity;
                            mapToAdd = map;
                        }
            }
            selectedMaps.Add(mapToAdd);
            chosenBehaviors.Add(mapToAdd.enemyBehavior);
            logger.LogEnemyComp(mapToAdd, EnemFitAndBehav.GetEnemyComposition(mapToAdd.GetAllEnemies()), 1-lowestSimilarity);

        }
        return selectedMaps;
    }

    //Finds the maps that has the most diverse exploration (geometry + furnishing) behavior comparetively to each other and the global average
    List<Map> SelectMostDiverseExplorationBehavior(List<Map> maps, int amountToSelect)
    {
        //Finds the average exploration behavior for all maps
        float[] averageExplorationBehavior = new float[3];
        List<Map> selectedMaps = new List<Map>();
        HashSet<Vector2> chosenBehaviors = new HashSet<Vector2>();
        foreach (var map in maps)
        {
            float[] mapExploreComposition = GetMapExplorationBehavior(map);
            for(int i=0; i<3; i++)
            {
                averageExplorationBehavior[i] += mapExploreComposition[i];
            }
        }
        for (int i=0; i<3; i++)
        {
            averageExplorationBehavior[i] /= (float) maps.Count;
            Debug.Log("avg ExploreBehave" + i + ": " + averageExplorationBehavior[i]);
        }
        //Add level with closest to average exploration behavior
        float highestSimilarity = 0f;
        Map mapToAdd = new Map();
        foreach (var map in maps)
        {
            float similarity = GetExplorationBehaviorDistance(averageExplorationBehavior, GetMapExplorationBehavior(map));
            if(similarity > highestSimilarity)
                {
                    highestSimilarity = similarity;
                    mapToAdd = map;
                }
        }
        selectedMaps.Add(mapToAdd);
        chosenBehaviors.Add(mapToAdd.geoBehavior);
        logger.LogExplorationParameters(mapToAdd, 1-highestSimilarity);

        //Add n (specified) amount of levels based on their difference in exploration behavior, compared to previously added maps.
        float lowestSimilarity;
        while (selectedMaps.Count < amountToSelect){
            lowestSimilarity = (float) selectedMaps.Count;
            foreach (var map in maps)
            {
                if (chosenBehaviors.Contains(map.geoBehavior)){
                    continue;
                }
                float similarity = 0;
                foreach(var selectedMap in selectedMaps){
                    similarity += GetExplorationBehaviorDistance(GetMapExplorationBehavior(selectedMap), GetMapExplorationBehavior(map));
                }
                similarity /= selectedMaps.Count;
                if(similarity < lowestSimilarity)
                        {
                            lowestSimilarity = similarity;
                            mapToAdd = map;
                        }
            }
            selectedMaps.Add(mapToAdd);
            chosenBehaviors.Add(mapToAdd.geoBehavior);
            logger.LogExplorationParameters(mapToAdd, 1-lowestSimilarity);
        }
        return selectedMaps;
    }

    List<Map> SelectMostDiverseEnemyBehaviorWithDifficulty(List<Map> maps, int amountToSelect)
    {
        float[] averageEnemyComposition = new float[5];
        List<Map> selectedMaps = new List<Map>();
        HashSet<Vector2> chosenBehaviors = new HashSet<Vector2>();
        
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
        chosenBehaviors.Add(mapToAdd.enemyBehavior);
        logger.LogEnemyComp(mapToAdd, EnemFitAndBehav.GetEnemyComposition(mapToAdd.GetAllEnemies()), 1-highestSimilarity);

        //Add n (specified) amount of maps, decided by difference in enemy composition to already added maps.
        float lowestSimilarity;
        float comparisonScore = 1;
        while (selectedMaps.Count < amountToSelect){
            lowestSimilarity = (float) selectedMaps.Count;
            comparisonScore = 1;
            foreach (var map in maps)
            {
                if (chosenBehaviors.Contains(map.enemyBehavior))
                {
                    continue;
                }
                float similarity = 0;
                float difficulty = 0;
                //Ensures that maps are different from each other all together and not just multiple maps in two extreme ends of the enemy composition scale
                foreach(var selectedMap in selectedMaps){
                    similarity += EnemFitAndBehav.GetCompositionSimilarity(EnemFitAndBehav.GetEnemyComposition(selectedMap.GetAllEnemies()), EnemFitAndBehav.GetEnemyComposition(map.GetAllEnemies()));
                    difficulty += Mathf.Abs(map.enemyBehavior.y - selectedMap.enemyBehavior.y) * 0.5f;
                }
                similarity /= selectedMaps.Count;
                difficulty /= selectedMaps.Count;
                difficulty = 1f / (1f + difficulty);
                var thisCompareScore = 0.7f * similarity + 0.3f * difficulty;

                if(thisCompareScore < comparisonScore)
                        {
                            comparisonScore = thisCompareScore;
                            mapToAdd = map;
                        }
            }
            selectedMaps.Add(mapToAdd);
            chosenBehaviors.Add(mapToAdd.enemyBehavior);
            logger.LogEnemyComp(mapToAdd, EnemFitAndBehav.GetEnemyComposition(mapToAdd.GetAllEnemies()), 1-comparisonScore);

        }
        return selectedMaps;
    }

    float[] GetMapExplorationBehavior(Map map)
    {
        return new float[] {map.geoBehavior.x, map.furnBehavior.x, map.furnBehavior.y};
    }


    float GetExplorationBehaviorDistance(float[] map1, float[] map2)
    {
        float absDiff = Mathf.Abs(map1[0] - map2[0]) + Mathf.Abs(map1[1] - map2[1]) + Mathf.Abs(map1[2] - map2[2]);
        return 1f - Mathf.Clamp01(absDiff / 16f);
    }




}