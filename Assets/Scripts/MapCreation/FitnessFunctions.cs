using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using static UnityEngine.EventSystems.EventTrigger;
//using static UnityEditor.PlayerSettings;
using System.ComponentModel;
using System;

public class FitnessFunctions : MonoBehaviour 
{

    private static bool IsFloor(int i)
    {
        if (i is (0 or 3 or 4 or 5 or 31 or 32)) return false;
        return true;
    }
    public static float GetGeometryFitness(MapCandidate candidate)
    {
        (float min, float max, float weight) optimalOpenness = (0.7f, 1f, 0.3f);
        (float min, float max, float weight) optimalMainToOptionalComponents = (0.5f, 2f, 0.2f);
        (int min, int max, float weight) optimalMapSize = (1000, 2000, 0.3f);
        (int min, int max, float weight) optimalComponentAmount = (2, 16, 0.1f);
        (float min, float max, float weight) optimalCorridorRatio = (0f, 0.1f, 0.1f);

        if (candidate.mapData.shortestPath == null || candidate.mapData.shortestPath.Count <=0) return 0;
        int mainCount = 1, optionalCount = 1;
        foreach (var c in candidate.mapData.components)
        {
            if (c.onMainPath) mainCount++;
            else optionalCount++;
        }

        float optimalOpennessScore = ScoreInterval(ComputeOpenness(candidate.mapData.mapArray), optimalOpenness.min, optimalOpenness.max);
        float optimalToMainScore = ScoreInterval((float)mainCount/optionalCount, optimalMainToOptionalComponents.min, optimalMainToOptionalComponents.max);
        float optimalMapSizeScore = ScoreInterval(candidate.mapData.mapSize, optimalMapSize.min, optimalMapSize.max);
        float optimalComponentAmountScore = ScoreInterval(candidate.mapData.components.Count, optimalComponentAmount.min, optimalComponentAmount.max);
        float optimalCorridorRatioScore = ScoreInterval((float)candidate.mapData.corridorTileCount/candidate.mapData.floorTiles.Count, optimalCorridorRatio.min, optimalCorridorRatio.max);
        return optimalOpennessScore * optimalOpenness.weight + optimalToMainScore * optimalMainToOptionalComponents.weight + optimalMapSizeScore * optimalMapSize.weight + optimalComponentAmountScore * optimalComponentAmount.weight + optimalCorridorRatioScore * optimalCorridorRatio.weight;
    }

    public static float GetFurnishingFitness(MapCandidate candidate)
    {
        (int min, int max, float weight) optimalSpread = (1, 6, 0.5f);
        float atEndWeight = 0.5f;

        float atEndScore = LootAtEndFitness(candidate.mapData);
        float spreadScore = FurnishingSpreadFitness(candidate, optimalSpread.min, optimalSpread.max);
        return spreadScore * optimalSpread.weight + atEndScore * atEndWeight;
    }

    public static float GetEnemyFitness(MapCandidate candidate)
    {
        return EnemyNotAtStartFitness(candidate.mapData);
    }

    static float ScoreInterval(float value, float min, float max, float falloff = 1.5f)
    {
        if (float.IsNaN(value) || float.IsInfinity(value) ||
            float.IsNaN(min) || float.IsNaN(max) ||
            float.IsInfinity(min) || float.IsInfinity(max))
            return 0f;

        if (max < min) (min, max) = (max, min);

        // Exact hit
        if (value >= min && value <= max) return 1f;

        // Distance outside band (normalized)
        float d = (value < min) ? (min - value) / Mathf.Max(min, 1e-6f)
                                : (value - max) / Mathf.Max(max, 1e-6f);

        // Smooth decay: 1 / (1 + d^falloff)
        return 1f / (1f + Mathf.Pow(d, falloff));
    }

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
                if (IsFloor(mapArray[x, y]))
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
                if (IsFloor(mapArray[x, y]))
                {
                    opennessScoreSum += LocalOpennessAt(4, x, y, mapArray);
                    amountOfTiles += 1f;
                }
            }
        }
        return opennessScoreSum / amountOfTiles;  // 0..1
    }


    public static float FurnishingSpreadFitness(MapCandidate candidate, int minPerRoom, int maxPerRoom)
    {
        var comps = candidate.mapData.components;

        if (comps == null || comps.Count == 0)
            return 0f;

        float totalFitness = 0f;
        int evaluatedRooms = 0;

        float range = Mathf.Max(1f, maxPerRoom - minPerRoom); // prevent divide-by-zero

        foreach (var c in comps)
        {
            int count = c.spikeCount + c.lootCount;

            float distance = 0f;

            // below minimum
            if (count < minPerRoom)
            {
                distance = minPerRoom - count;
            }
            // above maximum
            else if (count > maxPerRoom)
            {
                distance = count - maxPerRoom;
            }
            // inside valid band
            else
            {
                totalFitness += 1f;
                evaluatedRooms++;
                continue;
            }

            // normalize penalty relative to allowed band size
            float normalizedError = distance / range;

            // convert to fitness (soft penalty)
            float roomFitness = 1f - normalizedError;

            totalFitness += Mathf.Clamp01(roomFitness);
            evaluatedRooms++;
        }

        if (evaluatedRooms == 0)
            return 0f;

        return totalFitness / evaluatedRooms;
    }

    public static float LootAtEndFitness(MapInfo map)
    {
        float sum = 0f;
        int count = 0;

        foreach (var loot in map.furnishing)
        {
            if (loot.type == 0 || loot.type == 1) continue;
            var c = MapGenerator.GetComponentForTile(map, loot.placement);
            if (c == null) continue;
            if (!c.entryTile.HasValue || !c.exitTile.HasValue) continue;
            float dEntry = Mathf.Abs(loot.placement.x - c.entryTile.Value.x) + Mathf.Abs(loot.placement.y - c.entryTile.Value.y);
            float dEnd = Mathf.Abs(loot.placement.x - c.exitTile.Value.x) + Mathf.Abs(loot.placement.y - c.exitTile.Value.y);
            float denom = dEntry + dEnd;

            if (denom <= 0.0001f)
                continue;
            float p = dEntry / denom;

            float s = (p >= 0.6f) ? 1f : (p / 0.6f); // 0..1
            sum += s;
            count++;
        }

        if (count == 0) return 1f;
        return Mathf.Clamp01(sum / count);
    }


    public static float EnemyNotAtStartFitness(MapInfo map)
    {
        if (map == null || map.enemies == null)
            return 0f;

        if (!map.playerStartPos.HasValue)
            return 0f;

        if (map.enemies.Count == 0)
            return 0f;

        float sum = 0f;
        int count = 0;

        foreach (var enemy in map.enemies)
        {
            var c = MapGenerator.GetComponentForTile(map, enemy.placement);
            if (c == null) continue;
            if (!c.entryTile.HasValue || !c.exitTile.HasValue) continue;
            float dEntry = Mathf.Abs(enemy.placement.x - c.entryTile.Value.x) + Mathf.Abs(enemy.placement.y - c.entryTile.Value.y);
            float dEnd = Mathf.Abs(enemy.placement.x - c.exitTile.Value.x) + Mathf.Abs(enemy.placement.y - c.exitTile.Value.y);
            float denom = dEntry + dEnd;

            if (denom <= 0.0001f)
                continue;
            float p = dEntry / denom;

            float wantedDistance;
            if (enemy.type == 43 || enemy.type == 44) wantedDistance = 0.5f;
            else wantedDistance = 0.25f;

            float s = (p >= wantedDistance) ? 1f : (p / wantedDistance); // 0..1
            sum += s;
            count++;
        }

        if (count == 0) return 1f;
        return Mathf.Clamp01(sum / count);
    }
}
