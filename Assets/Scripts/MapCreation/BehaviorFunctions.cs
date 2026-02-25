using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class BehaviorFunctions : MonoBehaviour 
{

    // compute amount of components
    public static int GetComponentCountBehavior(MapCandidate candidate, int resolution)
    {
        int componentCount = candidate.mapData.components.Count;

        const int maxComponents = 20;   // max amount of components

        float normalized = Mathf.Clamp01((componentCount - 1f) / (maxComponents - 1f));

        return GetBehaviorRange(resolution, normalized);
    }

    static int GetBehaviorRange(int resolution, double value)
    {
        int bin = (int)(value * resolution);

        if (bin >= resolution)
            bin = resolution - 1;

        return bin;
    }

    public static Vector2 EnemyRoleDiversity(List<(Vector2Int placement, int type)> enemies, Vector2 behavior)
    {
        bool has0 = false;
        bool has1 = false;
        bool has2 = false;
        bool has3 = false;
        bool has4 = false;

        foreach (var enemy in enemies)
        {
            if (enemy.type == 0) has0 = true;
            else if (enemy.type == 1) has1 = true;
            else if (enemy.type == 2) has2 = true;
            else if (enemy.type == 3) has3 = true;
            else if (enemy.type == 4) has4 = true;
        }

        float typeCount = 0;
        if (has0) typeCount++;
        if (has1) typeCount++;
        if (has2) typeCount++;
        if (has3) typeCount++;
        if (has4) typeCount++;

        if (typeCount <= 1)
        {
            return new Vector2(0, behavior.y);
        }
        else if (typeCount <= 3)
        {
            return new Vector2(1, behavior.y);
        }
        else
        {
            return new Vector2(2, behavior.y);
        }
    }


    public static Vector2 EnemyClusterBehavior(MapInfo map, Vector2 behavior)
    {
        float averageClusterSize = 0;
        float clusterAmount = 0;
        foreach (var component in map.components)
        {
            foreach (var room in component.rooms){
                float clusterSize = 0;
                for (int a = room.XMin; a <= room.XMax; a++)
                {
                    for (int b = room.YMin; b <=room.YMax; b++)
                    {
                        /*if (map.mapArray[a, b] == 6 || map.mapArray[a, b] == 7)
                        {
                            clusterSize ++;
                        }*/
                        /*
                         ⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢀⣀⣀⣤⣀⣀⡀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
                        ⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣠⠔⠊⠉⠀⠀⠀⠀⠀⠈⠉⠒⢤⡀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
                        ⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⡤⠋⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠈⠳⣄⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
                        ⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⡼⠁⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢸⠤⠔⠒⠒⠛⠘⠓⠒⠲⠦⢄⠀⠀⠀⠀
                        ⠀⠀⠀⠀⠀⠀⠀⠀⠀⢠⠃⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢀⢞⡴⠍⠊⠉⠑⠂⡇⠀⡠⠀⡀⠀⠀⠀⠀⠀⠀⠙⢆⠀⠀
                        ⠀⠀⠀⠀⠀⠀⠀⠀⠀⢸⠀⠀⠀⠀⠀⠀⠀⠀⢀⠤⠒⠉⢈⠈⠁⠶⣤⠖⢠⠇⠀⠀⠀⠇⣆⠀⠀⠀⠀⠀⠀⠈⢧⠀
                        ⠀⠀⠀⠀⠀⠀⠀⠀⠀⢸⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣀⡤⢫⠂⠀⠀⠀⠀⢼⠀⠀⠀⠀⣄⠘⢦⡀⠀⠀⠀⠀⠀⠈⡆
                        ⠀⠀⠀⠀⠀⠀⠀⠀⠀⢸⠀⠀⠀⠀⠠⢖⣭⡾⠒⠈⠁⠀⠀⠀⠀⠀⠀⠀⠈⢣⠀⠀⠀⠈⠓⢄⠱⠀⠀⠀⠀⠀⠀⡇
                        ⠀⠀⠀⠀⠀⠀⠀⠀⠀⡔⠀⠀⡴⠛⠲⡴⢳⠀⠀⠀⠀⢀⡀⠀⠀⠀⠀⠀⠀⠀⡇⠀⠀⠀⠀⠈⣃⠤⠀⠀⠀⠀⢀⡇
                        ⠀⠀⠀⠀⠀⠀⠀⠀⡰⠁⠀⠀⢑⡤⠀⠁⠸⡀⠀⢀⡔⠊⠓⠒⠤⢄⣀⣀⣀⡴⠃⠀⠀⠀⠀⠐⠫⢄⣒⠤⠔⠀⢸⠁
                        ⠀⠀⠀⠀⠀⠀⠀⡸⠁⠀⠀⠰⡁⠀⡀⠀⠀⢧⠀⠸⡄⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠈⠀⠀⠀⠀⣠⠃⠀
                        ⠀⠀⠀⠀⠀⠀⣰⠁⠀⠀⠀⠀⠈⠉⢱⡀⠀⠈⢣⣀⠈⠒⠢⠤⣄⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⡴⠃⠀⠀
                        ⠀⠀⠀⠀⠀⢰⠃⠀⠀⠀⠀⠀⠀⠀⠀⢣⠀⠀⠀⠈⠉⠑⠒⠊⠁⠙⠦⣄⡀⠀⠀⠀⠀⠀⠀⠀⠀⣠⠴⡎⠀⠀⠀⠀
                        ⠀⠀⠀⠀⢀⠇⠀⠀⠀⠀⠀⠀⠀⠀⠀⠈⡆⠀⠀⠀⠀⠀⠀⠀⠀⠀⣀⠀⠈⠉⠛⠒⠒⠒⠒⠚⠉⠀⠀⡇⠀⠀⠀⠀
                        ⠀⠀⠀⠀⣸⠀⠀⠀⠀⠀⠀⢠⠀⠀⠀⠀⠘⠢⣄⠀⠀⠀⠀⠀⠀⠸⠅⠙⠔⠒⠒⡄⠀⠀⠀⠀⠀⠀⠀⡁⠀⠀⠀⠀
                        ⠀⠀⢀⣀⡇⠀⠀⠀⠀⠀⠀⠈⢦⠀⠀⠀⠀⠀⠀⠉⠙⠒⠒⠒⠤⠖⠁⠀⠀⠠⡤⠃⠀⠀⠀⠀⠀⠀⠀⡇⠀⠀⠀⠀
                        ⠀⡎⠁⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠳⢤⣀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢀⣠⠔⢆⣀⠜⠀⠀⠀⠀⠀⠀⠀⠀⡇⠀⠀⠀⠀
                        ⠀⡳⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠉⠉⠉⠒⠒⠒⢺⠋⠉⠁⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢠⠁⠀⠀⠀⠀
                        ⢸⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢸⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⡘⠀⠀⠀⠀⠀
                         */


                        int value = map.mapArray[a, b];
                        if (value >= 40 && value <= 44)
                        {
                            clusterSize++;
                        }
                    }
                }
                if (clusterSize > 0)
                {
                    averageClusterSize += clusterSize;
                    clusterAmount += 1;
                }
            }
        }
        averageClusterSize = averageClusterSize/clusterAmount;
        if (averageClusterSize <= 2)
        {
            return new Vector2(behavior.x, 0);
        }
        else if (averageClusterSize <= 3)
        {
            return new Vector2(behavior.x, 1);
        }
        else
        {
            return new Vector2(behavior.x, 2);
        }
    }

    public static int FurnishingBehaviorExploration(MapInfo map)
    {
        float lootCountOnMain = 0f;
        float lootCountOptional = 0f;

        foreach (var loot in map.furnishing)
        {
            // keep your exclusions
            if (loot.type == 0 || loot.type == 1)
                continue;

            var c = MapGenerator.GetComponentForTile(map, loot.placement);
            if (c == null)
                continue;

            if (c.onMainPath) lootCountOnMain++;
            else lootCountOptional++;
        }

        float total = lootCountOnMain + lootCountOptional;

        // No relevant loot found
        if (total <= 0f)
            return 5;

        float optionalShare = lootCountOptional / total;

        int score;
        if (optionalShare >= 0.80f) score = 0;
        else if (optionalShare >= 0.60f) score = 1;
        else if (optionalShare >= 0.40f) score = 2;
        else if (optionalShare >= 0.20f) score = 3;
        else score = 4;

        return score;
    }
}