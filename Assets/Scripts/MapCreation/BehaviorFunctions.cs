using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class BehaviorFunctions : MonoBehaviour 
{

    static float LocalOpennessAt(int opennessRadius, int posX, int posY, int[,] mapArray)
    {
        int floorCount = 0;
        int total = 0;

        // For each tile in R radius square, how many are floor?
        for (int dx = -opennessRadius; dx <= opennessRadius; dx++)
        {
            for (int dy = -opennessRadius; dy <= opennessRadius; dy++)
            {
                int x = posX + dx;
                int y = posY + dy;
                if (x < 0 || y < 0 || x >= mapArray.GetLength(0) || y >= mapArray.GetLength(1))
                    continue;

                total++;
                if (mapArray[x, y] != 2 && mapArray[x, y] != 0)
                    floorCount++;
            }
        }
        // Return percentage of floor tiles in radius
        if (total == 0) return 0f;
        return (float)floorCount / total;   // 0..1
    }


    static float ComputeOpenness(int[,] mapArray)
    {
        float opennessScoreSum = 0f;
        float amountOfTiles = 0f;

        for (int x = 0; x < mapArray.GetLength(0); x++)
        {
            for (int y = 0; y < mapArray.GetLength(1); y++)
            {
                if (mapArray[x, y] != 0 && mapArray[x, y] != 2)
                {
                    opennessScoreSum += LocalOpennessAt(5, x, y, mapArray);
                    amountOfTiles += 1f;
                }
            }
        }
        return opennessScoreSum / amountOfTiles;  // 0..1
    }

    public static int GetMapOpennessBehavior(MapCandidate candidate)
    {
        float openness = ComputeOpenness(candidate.mapData.mapArray);

        return GetBehaviorRange(0.35, 0.7, openness);
    }

    //
    static float ComputeWindingness(float shortestPathLength, Vector2Int start, Vector2Int end)
    {

        float straight =
            Mathf.Abs(start.x - end.x) +
            Mathf.Abs(start.y - end.y);

        float ratio = shortestPathLength / straight;

        float maxRatio = 3.0f;
        float windingness = (ratio - 1f) / (maxRatio - 1f);

        return Mathf.Clamp01(windingness);
    }

    public static int GetWindingnessBehavior(MapCandidate candidate)
    {
        int pathCount = candidate.mapData.shortestPath?.Count ?? 0;
        float wind = ComputeWindingness(pathCount, candidate.mapData.playerStartPos.Value, candidate.mapData.endPos.Value);
        return GetBehaviorRange(0.3, 0.7, wind);
    }


    static int GetBehaviorRange(double cutoff1, double cutoff2, double value)
    {
        if (value < cutoff1)
            return 1;
        else if (value < cutoff2)
            return 2;
        else
            return 3;
    }

    //Simple version for now, could be change to check for each room and do some sort of pseudo hashing clamped to intervals later
    public Vector2 EnemyCombatMix(List<(Vector2Int placement, int type)> enemies, Vector2 behavior)
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


    public Vector2 EnemyClusterBehavior(MapInfo map, Vector2 behavior)
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

    public Vector2 FurnishingBehaviorPickupDanger(MapInfo map, Vector2 behavior)
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

    public Vector2 FurnishingBehaviorExploration(MapInfo map, Vector2 behavior)
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