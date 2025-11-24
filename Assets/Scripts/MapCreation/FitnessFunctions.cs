using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class FitnessFunctions : MonoBehaviour
{

    [SerializeField] int opennessRadius = 3;
    [SerializeField] float minimumPathLength = 10;
    float LocalOpennessAt(int posX, int posY, int[,] mapArray)
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

    
    float ComputeOpenness(int[,] mapArray)
    {
        float opennessScoreSum = 0f;
        float amountOfTiles = 0f;

        for (int x = 0; x < mapArray.GetLength(0); x++)
        {
            for (int y = 0; y < mapArray.GetLength(1); y++)
            {
                if (mapArray[x, y] != 0 && mapArray[x, y] != 2)
                {
                    opennessScoreSum += LocalOpennessAt(x, y, mapArray);
                    amountOfTiles += 1f;
                }
            }
        }
        return opennessScoreSum / amountOfTiles;  // 0..1
    }

    float TrapPlacementScoreRoom(Room room, int[,] mapArray)
    {
        bool hasTrap = false;
        bool hasEnemy = false;
        float score = 0;
        for (int a = room.XMin; a <= room.XMax; a++)
        {
            for (int b = room.YMin; b <=room.YMax; b++)
            {
                if (mapArray[a, b] == 5)
                {
                    hasTrap = true;
                }
                if (mapArray[a, b] == 6 || mapArray[a, b] == 7)
                {
                    hasEnemy = true;
                }
                if (hasTrap && hasEnemy)
                {
                    return score = 1;
                }
            }
            } 
        return score;
    }


    public float LootPlacementFitnessRoom(Room room, MapInfo map)
    {
        float maxRoomScore = map.furnishing.Count/map.components.Count + 2.0f;
        float score = 0;
        for (int a = room.XMin; a <= room.XMax; a++)
        {
            for (int b = room.YMin; b <=room.YMax; b++)
            {
                if (map.mapArray[a, b] == 3 || map.mapArray[a, b] == 4)
                {
                    score += map.furnishing.Count/map.components.Count + 2.0f;
                }
                if (score >= maxRoomScore)
                {
                    return maxRoomScore;
                }
            }
            } 
        return score;
    }

    public float TrapPlacementFitnessTotal(MapInfo map)
    {
        float scoreTotal = 0;
        foreach (var component in map.components)
        {
            float trapScore = 0;
            float lootScore = 0;
            float enemyScore = 0;
            foreach (var room in component.rooms){
                trapScore += TrapPlacementScoreRoom(room, map.mapArray);
                lootScore += LootPlacementFitnessRoom(room, map);
                enemyScore += EnemiesClosenessToWallFitness(room, map);
            }
            //scoreTotal += trapScore/component.rooms.Count;
            //scoreTotal += lootScore/component.rooms.Count;
            scoreTotal += enemyScore/component.rooms.Count;
        }
    
        return scoreTotal/map.components.Count;
    }

    public float EnemiesClosenessToWallFitness(Room room, MapInfo map)
    {
        float score = 0;
        List<Vector2Int> enemyPlacement = new List<Vector2Int>();
        for (int a = room.XMin; a <= room.XMax; a++)
        {
            for (int b = room.YMin; b <=room.YMax; b++)
            {
                if (map.mapArray[a, b] == 6 || map.mapArray[a, b] == 7)
                {
                    enemyPlacement.Add(new Vector2Int(a,b));
                }
            }
            }
        foreach (var enemy in enemyPlacement)
        {
            if (map.mapArray[enemy.x, enemy.y] == 6 || map.mapArray[enemy.x, enemy.y] == 7)
            {
                float xDif;
                float yDif;
                if (math.abs(enemy.x - room.XMin) <= math.abs(enemy.x - room.XMax))
                {
                    xDif = math.abs(enemy.x - room.XMin);
                }
                else
                {
                     xDif = math.abs(enemy.x - room.XMax);
                }
                if (math.abs(enemy.y - room.YMin) <= math.abs(enemy.y - room.YMax))
                {
                    yDif = math.abs(enemy.y - room.YMin);
                }
                else
                {
                    yDif = math.abs(enemy.y - room.YMax);
                }
                //Diff less than 2 is irrelevant (keeps some posibility space so it doesn't always try to optimize for melee enemies being next to wall tile)
                if (xDif < 2)
                {
                    xDif = 2;
                }
                if (yDif < 2)
                {
                    yDif = 2;
                }
                if (map.mapArray[enemy.x, enemy.y] == 6)
                {
                    score += (1/xDif + 1/yDif)*0.5f;
                }
                else
                {
                    score += (1/(room.size.x * 0.5f) * xDif + 1/(room.size.y * 0.5f) * yDif) * 0.5f;
                }
             
            }
        }
        //Debug.Log("Score: " + score);
        return score; 
    }




}
