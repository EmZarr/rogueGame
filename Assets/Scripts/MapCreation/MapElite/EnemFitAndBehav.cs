using System;
using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;
using UnityEngine;
using h = FitnessAndBehaviorHelpers;

public static class EnemFitAndBehav
{
    private const float MaxDifficultyDensityForBehavior = 0.25f;
    // How many enemies does loot counteract
    private const float lootCounteract = 0.5f;

    public static (float fitness, (int enemyType, int difficulty) behavior) GetEnemyFitnessAndBehavior(Map map)
    {

        var (behavior, behaviorConsistency, difficultyPerRoom) = GetEnemBehav(map);

        return (GetEnemyFitness(map, behaviorConsistency, difficultyPerRoom), behavior);
    }

    private static float GetEnemyFitness(Map map, float behaviorConsistency, List<float> difficultyPerRoom)
    {
        (float score, float weight) consistencyScore = (behaviorConsistency, 0.6f);
        (float score, float weight) difficultyScalingScore = (GetDifficultyScalingScore(map, difficultyPerRoom), 0.4f);

        return difficultyScalingScore.score * difficultyScalingScore.weight
             + consistencyScore.score * consistencyScore.weight;
    }

    private static ((int enemyType, int difficulty) behavior, float behaviorConsistency, List<float> difficultyPerRoom) GetEnemBehav(Map map)
    {
        if (map == null || map.rooms == null || map.rooms.Count == 0)
            return ((0, 0), 0f, new List<float>());

        var allEnemies = map.GetAllEnemies();

        List<float> difficultyPerRoom = new List<float>(map.rooms.Count);
        map.enemyComp = GetEnemyComposition(allEnemies);

        float difficultyDensitySum = 0f;
        float compositionConsistencySum = 0f;

        foreach (var room in map.rooms)
        {
            // How different is room comp from map comp
            compositionConsistencySum += GetCompositionSimilarity(map.enemyComp, GetEnemyComposition(room.enemies));

            // Total room difficulty:
            // - enemies count by their actual used budget
            // - loot counts negative portion enemy
            float roomDifficulty =
                room.enemyBudgetUsed -
                room.loot.Count * lootCounteract;

            // Difficulty per tile in the room
            float difficultyDensity = roomDifficulty / room.tiles.Count;

            // Normalize so 20% = 1, 0% = 0
            float normalizedDifficultyDensity = Mathf.Clamp01(difficultyDensity / MaxDifficultyDensityForBehavior);

            difficultyPerRoom.Add(normalizedDifficultyDensity);
            difficultyDensitySum += normalizedDifficultyDensity;
        }


        float averageRoomDifficultyDensity = difficultyDensitySum / map.rooms.Count;

        float behaviorConsistency = compositionConsistencySum/map.rooms.Count;

        map.difficulty = h.GetBehaviorRange(4, averageRoomDifficultyDensity);
        return ((EnemyRoleCompositionBehavior(allEnemies, 20), map.difficulty),behaviorConsistency, difficultyPerRoom);
    }

    private static float GetDifficultyScalingScore(Map map, List<float> difficultyPerRoom)
    {
        if (map == null || map.rooms == null || map.rooms.Count == 0 ||
            difficultyPerRoom == null || difficultyPerRoom.Count != map.rooms.Count)
            return 0f;

        // Collect main-path rooms with their difficulty
        List<(int orderIndex, float difficulty)> mainPathDifficulties = new();

        for (int i = 0; i < map.rooms.Count; i++)
        {
            Room room = map.rooms[i];

            if (room == null || !room.onMainPath)
                continue;

            mainPathDifficulties.Add((room.orderIndex, difficultyPerRoom[i]));
        }

        // Need at least 2 rooms to evaluate scaling
        if (mainPathDifficulties.Count <= 1)
            return 1f;

        // Sort by progression order
        mainPathDifficulties.Sort((a, b) => a.orderIndex.CompareTo(b.orderIndex));

        int goodPairs = 0;
        int totalPairs = 0;

        // Compare every earlier room to every later room
        for (int i = 0; i < mainPathDifficulties.Count; i++)
        {
            for (int j = i + 1; j < mainPathDifficulties.Count; j++)
            {
                totalPairs++;

                // Good if later room is at least as hard as earlier room
                if (mainPathDifficulties[j].difficulty >= mainPathDifficulties[i].difficulty)
                    goodPairs++;
            }
        }

        if (totalPairs == 0)
            return 1f;

        return (float)goodPairs / totalPairs;
    }

    private static float[] GetEnemyComposition(List<GridEntry> enemies)
    {
        float[] composition = new float[MapHelpers.EnemyTypes.Length];

        if (enemies == null || enemies.Count == 0)
            return composition;

        int total = 0;

        foreach (var e in enemies)
        {
            int t = e.type;
            composition[t]++;
            total++;
        }

        if (total == 0)
            return composition;

        for (int i = 0; i < composition.Length; i++)
        {
            composition[i] /= total;
        }

        return composition;
    }

    // Returns:
    // 1 → identical compositions
    // 0 → completely different compositions
    private static float GetCompositionSimilarity(float[] a, float[] b)
    {
        if (a == null || b == null || a.Length != b.Length)
            return 0f;

        float diff = 0f;

        for (int i = 0; i < a.Length; i++)
        {
            diff += Mathf.Abs(a[i] - b[i]);
        }

        // Max possible diff between two normalized distributions is 2
        float normalizedDiff = diff / 2f;

        return 1f - Mathf.Clamp01(normalizedDiff);
    }

    // Uses composition ranking to reduce search space to 1000 for resolution 10, 126 for resolution 20
    public static int EnemyRoleCompositionBehavior(List<GridEntry> enemies, int resolution)
    {
        if (enemies == null || enemies.Count == 0)
            return 0;

        // Count amount of each
        int[] counts = new int[MapHelpers.EnemyTypes.Length];
        int total = 0;

        foreach (var e in enemies)
        {
            int t = e.type;
            counts[t]++;
            total++;
        }

        if (total == 0)
            return 0;

        // Step size in percent (e.g. 10 => 10% steps) => N units total
        // N must be an integer, so resolution must divide 100.
        if (resolution <= 0 || (100 % resolution) != 0)
        {
            Debug.LogWarning($"EnemyRoleCompositionBehavior: resolution must be a positive divisor of 100. Got {resolution}.");
            return 0;
        }

        int N = 100 / resolution; // e.g. 10 units for 10% steps

        // Quantize ratios into integer units that sum EXACTLY to N (largest remainder)
        int[] units = QuantizeToUnitsLargestRemainder(counts, total, N);
        // Encode composition uniquely into 0..C(N+4,4)-1
        return (int)RankWeakComposition(units, N);
    }

    static int[] QuantizeToUnitsLargestRemainder(int[] counts, int total, int N)
    {
        int k = counts.Length; // 5
        int[] units = new int[k];
        float[] rema = new float[k];

        int sum = 0;
        for (int i = 0; i < k; i++)
        {
            float exact = (counts[i] / (float)total) * N; // exact units in [0..N]
            int floor = Mathf.FloorToInt(exact);
            units[i] = floor;
            rema[i] = exact - floor;
            sum += floor;
        }

        // Distribute remaining units to largest remainders until sum == N
        int remaining = N - sum;
        while (remaining > 0)
        {
            int best = 0;
            for (int i = 1; i < k; i++)
                if (rema[i] > rema[best]) best = i;

            units[best]++;
            rema[best] = -1f; // mark consumed for this pass
            remaining--;
        }

        return units;
    }

    // Rank of a weak composition of N into k parts (here k=5).
    // Uses stars-and-bars: map composition -> combination of bar positions -> combinadic rank.
    static long RankWeakComposition(int[] parts, int N)
    {
        int k = parts.Length;
        int sum = 0;
        for (int i = 0; i < k; i++) sum += parts[i];
        if (sum != N) throw new ArgumentException($"Parts must sum to {N} (got {sum}).");

        int nSlots = N + k - 1; // stars + bars
        int r = k - 1;          // number of bars

        // bar positions (0-indexed): b_i = (p0+...+p_i) + i   for i=0..k-2
        int[] bars = new int[r];
        int prefix = 0;
        for (int i = 0; i < r; i++)
        {
            prefix += parts[i];
            bars[i] = prefix + i;
        }

        return RankCombination(nSlots, r, bars);
    }

    // Lexicographic rank of the chosen indices (combinadic-style).
    static long RankCombination(int n, int r, int[] chosen)
    {
        long rank = 0;
        int prev = -1;

        for (int i = 0; i < r; i++)
        {
            for (int x = prev + 1; x < chosen[i]; x++)
            {
                rank += nCk(n - 1 - x, r - 1 - i);
            }
            prev = chosen[i];
        }

        return rank;
    }

    static long nCk(int n, int k)
    {
        if (k < 0 || k > n) return 0;
        if (k == 0 || k == n) return 1;

        k = Math.Min(k, n - k);
        long result = 1;

        for (int i = 1; i <= k; i++)
        {
            result = (result * (n - (k - i))) / i;
        }

        return result;
    }
}