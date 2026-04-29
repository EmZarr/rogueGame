using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using static UnityEngine.EventSystems.EventTrigger;
//using static UnityEditor.PlayerSettings;
using System.ComponentModel;
using System;
using h = FitnessAndBehaviorHelpers;

public static class GeoFitAndBehav
{
    public static (float fitness, int behavior) GetGeoFitnessAndBehavior(Map map)
    {

        (int behavior, float behaviorConsistency) = GetGeoBehavior(map);

        return (GetGeometryFitness(map, behaviorConsistency), behavior);
    }
    private static float GetGeometryFitness(Map map, float behaviorConsistancy)
    {
        // We want 20% to 50% of rooms to be optional
        (float min, float max, float weight) optimalMainToOptionalComponents = (0.2f, 0.5f, 0.2f);

        // Certain map sizes
        (int min, int max, float weight) optimalMapSize = (750, 1000, 0.2f);

        // We dont want more than 10% of tiles to be corridors
        (float min, float max, float weight) optimalCorridorRatio = (0f, 0.1f, 0.2f);

        (float score, float weight) consistancyScore = (behaviorConsistancy, 0.4f);

        float optionalRatio = (float)(map.rooms.Count - map.mainPathRooms.Count) / map.rooms.Count;
        float optimalToMainScore = h.ScoreInterval(
            optionalRatio,
            optimalMainToOptionalComponents.min,
            optimalMainToOptionalComponents.max
        );

        float optimalMapSizeScore = h.ScoreInterval(map.TotalTileCount(), optimalMapSize.min, optimalMapSize.max);

        // Times two since corridor is 2 tiles wide
        float optimalCorridorRatioScore = h.ScoreInterval((float)(map.TotalCorridorLength()*2)/map.TotalTileCount(), optimalCorridorRatio.min, optimalCorridorRatio.max);

        return optimalToMainScore * optimalMainToOptionalComponents.weight + optimalMapSizeScore * optimalMapSize.weight + optimalCorridorRatioScore * optimalCorridorRatio.weight + consistancyScore.score *consistancyScore.weight;
    }

    // Finds the geo behavior and behavior consistancy (for fitness)
    private static (int behavior, float behaviorConsistency) GetGeoBehavior(Map map)
    {
        if (map == null || map.rooms == null || map.rooms.Count == 0)
            return (0, 0f);

        // List of scores used for consistancy
        List<float> roomOpennessScores = new List<float>(map.rooms.Count);

        // find average score and score for each rooms
        float opennessSum = 0f;
        float weightedOpennessSum = 0f;
        int totalTiles = 0;

        foreach (Room room in map.rooms)
        {
            float roomOpenness = ComputeRoomOpenness(room);
            roomOpennessScores.Add(roomOpenness);
            opennessSum += roomOpenness;
            weightedOpennessSum += roomOpenness * room.tiles.Count;
            totalTiles+= room.tiles.Count;

        }

        float averageOpenness = weightedOpennessSum / totalTiles;
        float behaviorConsistency = h.GetConsistencyScore(roomOpennessScores, averageOpenness);

        return (h.GetBehaviorRangeSmooth(12, averageOpenness, 0.18f, 0.9f), behaviorConsistency);
    }

    // Computes how "open" a single tile is within a room
    // Openness = how many neighboring positions are also floor tiles
    static float LocalOpennessAt(Room room, int opennessRadius, Vector2Int pos)
    {
        int floorCount = 0; // number of nearby tiles that are part of the room
        int total = 0;      // total number of positions checked

        // Loop over a square area centered on this tile
        // Example: radius = 4 → checks a 9x9 area
        for (int dx = -opennessRadius; dx <= opennessRadius; dx++)
        {
            for (int dy = -opennessRadius; dy <= opennessRadius; dy++)
            {
                // Compute the neighbor position
                Vector2Int p = new Vector2Int(pos.x + dx, pos.y + dy);

                total++; // count every position we check

                // If this position is part of the room (i.e., a floor tile),
                // we count it as "open"
                if (room.tileSet.Contains(p))
                    floorCount++;
            }
        }

        // Safety check (should never really happen)
        if (total == 0) return 0f;
        // Return fraction of nearby tiles that are floor tiles (0..1)
        return (float)floorCount / total;
    }

    // Computes the average openness of an entire room
    // by averaging openness over all its tiles
    static float ComputeRoomOpenness(Room room, int opennessRadius = 6)
    {
        // Guard against invalid room
        if (room == null || room.tiles == null || room.tiles.Count == 0)
            return 0f;

        float opennessScoreSum = 0f;

        // For every tile in the room...
        foreach (var tile in room.tiles)
        {
            // Compute how open that tile is locally
            opennessScoreSum += LocalOpennessAt(room, opennessRadius, tile.pos);
        }

        // Return the average openness across all tiles
        return opennessScoreSum / room.tiles.Count;
    }
}
