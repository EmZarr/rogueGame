using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class BehaviorFunctions : MonoBehaviour
{
    

    //Simple version for now, could be change to check for each room and do some sort of pseudo hashing clamped to intervals later
    public Vector2 EnemyCombatMix(List<(Vector2Int placement, int type)> enemies)
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
        if (RangedCount/MeleeCount <= 0.2)
        {
            return new Vector2(0, 0);
        }
        else if (RangedCount/MeleeCount <= 0.4)
        {
            return new Vector2(1, 0);
        }
        else if (RangedCount/MeleeCount <= 0.6)
        {
            return new Vector2(2, 0);
        }
        else if (RangedCount/MeleeCount <= 0.8)
        {
            return new Vector2(3, 0);
        }
        else if (RangedCount/MeleeCount <= 1.0)
        {
            return new Vector2(4, 0);
        }
        else if (RangedCount/MeleeCount <= 1.2)
        {
            return new Vector2(5, 0);
        }
        else if (RangedCount/MeleeCount <= 1.4)
        {
            return new Vector2(6, 0);
        }
        else if (RangedCount/MeleeCount <= 1.6)
        {
            return new Vector2(7, 0);
        }
        else
        {
            return new Vector2(8, 0);
        }
    }


    public Vector2 EnemyClusterBehavior(MapInfo map)
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
            return new Vector2(0, 0);
        }
        else if (averageClusterSize <= 2.5)
        {
            return new Vector2(1, 0);
        }
        else if (averageClusterSize <= 3)
        {
            return new Vector2(2, 0);
        }
        else if (averageClusterSize <= 3.5)
        {
            return new Vector2(3, 0);
        }
        else if (averageClusterSize <= 4)
        {
            return new Vector2(4, 0);
        }
        else if (averageClusterSize <= 4.5)
        {
            return new Vector2(5, 0);
        }
        else if (averageClusterSize <= 5)
        {
            return new Vector2(6, 0);
        }
        else if (averageClusterSize <= 5.5)
        {
            return new Vector2(7, 0);
        }
        else
        {
            return new Vector2(8, 0);
        }
    }

    public Vector2 FurnishingBehavior(MapInfo map)
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
            return new Vector2(0, 0);
        }
        else if (averageDistance <= 2.5)
        {
            return new Vector2(1, 0);
        }
        else if (averageDistance <= 3)
        {
            return new Vector2(2, 0);
        }
        else if (averageDistance <= 3.5)
        {
            return new Vector2(3, 0);
        }
        else if (averageDistance <= 4)
        {
            return new Vector2(4, 0);
        }
        else if (averageDistance <= 4.5)
        {
            return new Vector2(5, 0);
        }
        else if (averageDistance <= 5)
        {
            return new Vector2(6, 0);
        }
        else if (averageDistance <= 5.5)
        {
            return new Vector2(7, 0);
        }
        else
        {
            return new Vector2(8, 0);
        }
    }

}