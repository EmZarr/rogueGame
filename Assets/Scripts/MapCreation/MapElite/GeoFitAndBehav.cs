using System.Collections.Generic;
using UnityEngine;
using h = FitnessAndBehaviorHelpers;



// Fitness and behavior for the geometry stage of our MAP-Elites.

// Behavior: average "openness" of all rooms.
// Fitness is a weighted sum of:
//   - share of rooms that are optional (around: 20–50%)
//   - total tile count (around: 750–1000)
//   - corridor share of total tiles (under 10%)
//   - how consistent room openness is across the map

public static class GeoFitAndBehav
{
    // Entry point used by the MAP-Elites.
    // Returns:
    // - fitness: scalar in [0,1], the higher the better
    // - behavior: openness bin index [0, 12]

    public static (float fitness, int behavior) GetGeoFitnessAndBehavior(Map map)
    {
        (int behavior, float behaviorConsistency) = GetGeoBehavior(map);
        return (GetGeometryFitness(map, behaviorConsistency), behavior);
    }

    // Each goal contributes a [0,1] score (via ScoreInterval) which is multiplied by its weight.
    private static float GetGeometryFitness(Map map, float behaviorConsistency)
    {
        // We want 20% to 50% of rooms to be optional.
        (float min, float max, float weight) optimalMainToOptionalComponents = (0.2f, 0.5f, 0.2f);

        // Total map size between 750 and 1000 tiles.
        (int min, int max, float weight) optimalMapSize = (750, 1000, 0.2f);

        // Corridors shouldn't make up more than 10% of tiles.
        (float min, float max, float weight) optimalCorridorRatio = (0f, 0.1f, 0.2f);

        // Room openness should be consistent across the map.
        (float score, float weight) consistencyScore = (behaviorConsistency, 0.4f);

        // Score the actual map on each of our goals.
        float optionalRatio = (float)(map.rooms.Count - map.mainPathRooms.Count) / map.rooms.Count;
        float optimalToMainScore = h.ScoreInterval(
            optionalRatio,
            optimalMainToOptionalComponents.min,
            optimalMainToOptionalComponents.max
        );

        float optimalMapSizeScore = h.ScoreInterval(map.TotalTileCount(), optimalMapSize.min, optimalMapSize.max);

        // ×2 because each corridor segment is 2 tiles wide.
        float optimalCorridorRatioScore = h.ScoreInterval((float)(map.TotalCorridorLength()*2)/map.TotalTileCount(), optimalCorridorRatio.min, optimalCorridorRatio.max);

        return optimalToMainScore * optimalMainToOptionalComponents.weight + optimalMapSizeScore * optimalMapSize.weight + optimalCorridorRatioScore * optimalCorridorRatio.weight + consistencyScore.score * consistencyScore.weight;
    }

    // Finds the geo behavior and behavior consistency (for fitness)
    // The behavior axis is weighted by room size: a big open room contributes more to averageOpenness than a small one.
    // Consistency uses unweighted per-room scores.
    private static (int behavior, float behaviorConsistency) GetGeoBehavior(Map map)
    {
        if (map == null || map.rooms == null || map.rooms.Count == 0) return (0, 0f);

        // Per-room scores collected here for consistency
        List<float> roomOpennessScores = new List<float>(map.rooms.Count);

        // Two running sums:
        // one unweighted (for consistency)
        // weighted by room size (for the behavior axis).
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

        // Smoothstep binning means the middle of [0.18, 0.9] gets finer resolution than the edges.
        return (h.GetBehaviorRangeSmooth(12, averageOpenness, 0.18f, 0.9f), behaviorConsistency);
    }

    // Computes how "open" a single tile is within a room
    // Openness = how many neighboring positions are also floor tiles

    // opennessRadius = 6 -> 13×13 window = 169 cells.
    // If 100 of those are in the room -> openness = 0.59
  
    // Tiles in the middle of a big room -> openness near 1
    // Tiles in a narrow corridor -> openness near 0
    static float LocalOpennessAt(Room room, int opennessRadius, Vector2Int pos)
    {
        int floorCount = 0; // Number of nearby tiles that are part of the room.
        int total = 0;      // Total number of positions checked.

        // Loop over a square area centered on this tile.
        for (int dx = -opennessRadius; dx <= opennessRadius; dx++)
        {
            for (int dy = -opennessRadius; dy <= opennessRadius; dy++)
            {
                // Compute the neighbor position.
                Vector2Int p = new Vector2Int(pos.x + dx, pos.y + dy);

                total++;

                // If this position is part of the room, we count it as "open".
                if (room.tileSet.Contains(p)) floorCount++;
            }
        }

        // Safety check
        if (total == 0) return 0f;
        // Return fraction of nearby tiles that are floor tiles (0..1)
        return (float)floorCount / total;
    }

    // Computes the openness of an entire room.
    // Averages LocalOpennessAt over every tile in the room.
    // A square 20x20 room -> openness is around 0.7
    // A long thin corridor -> openness is around 0.2
    // A small room -> openness is around 0.4

    static float ComputeRoomOpenness(Room room, int opennessRadius = 6)
    {
        // Guard against invalid room
        if (room == null || room.tiles == null || room.tiles.Count == 0) return 0f;

        float opennessScoreSum = 0f;

        foreach (var tile in room.tiles)
        {
            opennessScoreSum += LocalOpennessAt(room, opennessRadius, tile.pos);
        }

        return opennessScoreSum / room.tiles.Count;
    }
}
