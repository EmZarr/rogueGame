using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using h = FitnessAndBehaviorHelpers;

public static class FurnFitAndBehav
{
    // We treat 15% density as "max interesting" for binning purposes.
    // Anything above this is clamped into the top bin.
    private const float MaxLootDensityForBehavior = 0.15f;
    private const float MaxObstacleDensityForBehavior = 0.15f;

    public static (float fitness, (int lootDensity, int obstacleDensity) behavior) GetFurnFitnessAndBehavior(Map map)
    {

        var (behavior, behaviorConsistency) = GetFurnBehav(map);

        return (GetFurnishingFitness(map, behaviorConsistency), behavior);
    }
    private static float GetFurnishingFitness(Map map, float behaviorConsistency)
    {
        (float score, float weight) consistancyScore = (behaviorConsistency, 1f);
        return consistancyScore.score * consistancyScore.weight;
    }

    private static ((int lootDensity, int obstacleDensity) behavior, float behaviorConsistency) GetFurnBehav(Map map)
    {
        if (map == null || map.rooms == null || map.rooms.Count == 0)
            return ((0, 0), 0f);

        List<float> roomLootScores = new List<float>(map.rooms.Count);
        List<float> roomObstacleScores = new List<float>(map.rooms.Count);
        float lootDensitySum = 0f;
        float obstacleDensitySum = 0f;

        foreach (var room in map.rooms) {
            // Chance that a tile in room has type on it
            float lootDensity = (float)room.loot.Count / room.tiles.Count;
            float obstacleDensity = (float)room.obstacles.Count / room.tiles.Count;

            // 15% = 1, 0% = 0.
            float normalizedLootDensity = Mathf.Clamp01(lootDensity / MaxLootDensityForBehavior);
            float normalizedObstacleDensity = Mathf.Clamp01(obstacleDensity / MaxObstacleDensityForBehavior);

            roomLootScores.Add(normalizedLootDensity);
            roomObstacleScores.Add(normalizedObstacleDensity);

            lootDensitySum += normalizedLootDensity;
            obstacleDensitySum += normalizedObstacleDensity;
        }

        float averageRoomLootDensity = lootDensitySum/map.rooms.Count;
        float averageRoomObstacleDensity = obstacleDensitySum/map.rooms.Count;

        float behaviorConsistency = (h.GetConsistencyScore(roomLootScores, averageRoomLootDensity) + h.GetConsistencyScore(roomObstacleScores, averageRoomObstacleDensity)) / 2;

        return ((h.GetBehaviorRange(4, averageRoomLootDensity), h.GetBehaviorRange(4, averageRoomObstacleDensity)),behaviorConsistency);
    }
}