using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class FitnessFunctions : MonoBehaviour
{
    public float GetGeometryFitness(MapCandidate candidate, (int min, int max, float weight) optimalPathLength, (int min, int max, float weight) optimalMainToOptionalComponents, (int min, int max, float weight) optimalMapSize, (int min, int max, float weight) optimalComponentAmount)
    {
        float pathLengthScore = ScoreInterval(candidate.mapData.shortestPath.Count, optimalPathLength.min, optimalPathLength.max);

        (int mainCount, int optionalCount) mainAndOptionalCount = (0, 0);
        foreach (var component in candidate.mapData.components)
        {
            if (component.onMainPath)
            {
                mainAndOptionalCount.mainCount++;
            }
            else
            {
                mainAndOptionalCount.optionalCount++;
            }
        }
        float optimalToMainScore = ScoreInterval(mainAndOptionalCount.mainCount/mainAndOptionalCount.optionalCount, optimalMainToOptionalComponents.min, optimalMainToOptionalComponents.max);

        float optimalMapSizeScore = ScoreInterval(candidate.mapData.mapSize, optimalMapSize.min, optimalMapSize.max);

        float optimalComponentAmountScore = ScoreInterval(candidate.mapData.components.Count, optimalComponentAmount.min, optimalComponentAmount.max);


        return pathLengthScore * optimalPathLength.weight + optimalToMainScore * optimalMainToOptionalComponents.weight + optimalMapSizeScore * optimalMapSize.weight + optimalComponentAmountScore * optimalComponentAmount.weight;
    }

    float ScoreInterval(float value, int min, int max)
    {
        if (min > max)
        {
            int tmp = min;
            min = max;
            max = tmp;
        }

        if (value <= 0f)
            return 0f;

        if (value < min)
        {
            // 0..1 as we approach min from below
            return Mathf.Clamp01(value / min);
        }

        if (value > max)
        {
            // 1..0 as we exceed max
            return Mathf.Clamp01(max / value);
        }

        // perfect if inside [min, max]
        return 1f;
    }
    
    float RoomFitnessFurEne(Room room, MapInfo map)
    {
        bool hasTrap = false;
        bool hasEnemy = false;
        float score = 0;
        float furnishScore = 0;
        float maxRoomScore = map.furnishing.Count/map.components.Count + 2.0f;
        List<Vector2Int> enemyPlacement = new List<Vector2Int>();
        for (int a = room.XMin; a <= room.XMax; a++)
        {
            for (int b = room.YMin; b <=room.YMax; b++)
            {
                if (map.mapArray[a, b] == 3 || map.mapArray[a, b] == 4)
                {
                    furnishScore += map.furnishing.Count/map.components.Count + 2.0f;
                }
                if (map.mapArray[a, b] == 5)
                {
                    hasTrap = true;
                }
                if (map.mapArray[a, b] == 6 || map.mapArray[a, b] == 7)
                {
                    hasEnemy = true;
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
        if (hasEnemy && hasTrap)
        {
            score+=1;
        }
        if (furnishScore > maxRoomScore)
        {
            furnishScore = maxRoomScore;
        }
        score+= furnishScore;
        return score;
    }


    public float RoomFitnessTotal(MapInfo map)
    {
        float scoreTotal = 0;
        foreach (var component in map.components)
        {
            float roomScore = 0;
            foreach (var room in component.rooms){
                roomScore += RoomFitnessFurEne(room, map);
            }
            scoreTotal += roomScore/component.rooms.Count;
        }
    
        return scoreTotal/map.components.Count;
    }


    //Moved old functions to bottom in case something breaks can be deleted soon if no issues.
    /*public float EnemiesClosenessToWallFitness(Room room, MapInfo map)
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
    }*/
}
