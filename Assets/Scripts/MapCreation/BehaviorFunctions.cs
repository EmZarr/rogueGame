using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class BehaviorFunctions : MonoBehaviour 
{
    
    // compute amount of components
    public static int GetComponentCountBehavior(MapCandidate candidate, int resolution)
    {
        int componentCount = candidate.mapData.components.Count;
        float normalized = Mathf.Clamp01((componentCount - 1f) / 5f);
        return GetBehaviorRange(resolution, normalized);
    }

    static int GetBehaviorRange(int resolution, double value)
    {
        int bin = (int)(value * resolution);

        if (bin >= resolution)
            bin = resolution - 1;

        return bin;
    }

    //Simple version for now, could be change to check for each room and do some sort of pseudo hashing clamped to intervals later
    public static Vector2 EnemyCombatMix(List<(Vector2Int placement, int type)> enemies, Vector2 behavior)
    {
        float RangedCount = 0;
        float MeleeCount = 0;
        float behaviorScore = 0;
        foreach (var enemy in enemies)
        {
            if (enemy.type == 6)
            {
                MeleeCount++;
            }
            else
            {
                RangedCount++;
            }
        }
        if (RangedCount/MeleeCount <= 0.5)
        {
            return new Vector2(0, behavior.y);
        }
        else if (RangedCount/MeleeCount <= 1.0)
        {
            return new Vector2(1, behavior.y);
        }
        else 
        {
            return new Vector2(2, behavior.y);
        }
        
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
            return new Vector2(behavior.x, 0);
        }
        else if (typeCount <= 3)
        {
            return new Vector2(behavior.x, 1);
        }
        else
        {
            return new Vector2(behavior.x, 2);
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
                        if (map.mapArray[a, b] == 6 || map.mapArray[a, b] == 7)
                        {
                            clusterSize ++;
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


    public static Vector2 FurnishingBehaviorPickupDanger(MapInfo map, Vector2 behavior)
    {
        float averageDistance = 0;
        int counter = 0;
        //I hate, but I must zzzz (check each enemy/trap for each loot)
        foreach (var loot in map.furnishing)
        {
            float distance = 100.0f;
            if (loot.type == 3 || loot.type == 4)
            {
                foreach (var enemy in map.enemies)
                {
                    if((loot.placement - enemy.placement).sqrMagnitude < distance)
                    {
                        distance = (loot.placement - enemy.placement).sqrMagnitude;
                    }
                }

                foreach (var trap in map.furnishing)
                {
                    if (trap.type == 5)
                    {
                        if((loot.placement - trap.placement).sqrMagnitude < distance)
                    {
                        distance = (loot.placement - trap.placement).sqrMagnitude;
                    }
                    }
                }
            }
            averageDistance += distance;
            counter++;
        }
        averageDistance = averageDistance/counter;
        if (averageDistance <= 2)
        {
            return new Vector2(0, behavior.y);
        }
        else if (averageDistance <= 4)
        {
            return new Vector2(1, behavior.y);
        }
        else
        {
            return new Vector2(2, behavior.y);
        }
    }

    public static Vector2 FurnishingBehaviorExploration(MapInfo map, Vector2 behavior)
    {
        float lootCountOnMain = 0;
        float lootCountOptional = 0;
        foreach (var loot in map.furnishing)
        {
            if (loot.type != 5)
            {
                foreach (var component in map.components)
                {
                    if (component.ContainsTile(loot.placement))
                    {
                        if (component.onMainPath)
                        {
                            lootCountOnMain++;
                        }
                        else
                        {
                            lootCountOptional++;
                        }
                        break;
                    }
                }
            }
        }
        float ratio = lootCountOnMain/lootCountOptional;
        if (ratio <= 0.6f)
        {
            return new Vector2(behavior.x, 0);
        }
        else if (ratio <= 1.1f)
        {
            return new Vector2(behavior.x, 1);
        }
        else
        {
            return new Vector2(behavior.x, 2);
        }
    }
}