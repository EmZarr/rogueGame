using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using System;

public class BehaviorFunctions : MonoBehaviour 
{

    // compute amount of components
    public static int OpennessByRoomSizeShare(MapInfo map, int resolution)
    {
        if (map?.components == null || map.components.Count == 0) return 0;

        int totalTiles = 0;
        List<int> sizes = new();

        foreach (var c in map.components)
        {
            int s = (c.tiles != null) ? c.tiles.Count : 0;
            if (s <= 0) continue;
            sizes.Add(s);
            totalTiles += s;
        }
        if (sizes.Count == 0 || totalTiles == 0) return 0;

        sizes.Sort((a, b) => b.CompareTo(a)); // descending
        int k = Mathf.Min(3, sizes.Count);
        int top = 0;
        for (int i = 0; i < k; i++) top += sizes[i];

        float topShare = top / (float)totalTiles; // 0..1
                                                  // Interpret: higher topShare => fewer/larger rooms (more open-feeling)
        return GetBehaviorRange(resolution, Mathf.Clamp01(topShare));
    }

    static int GetBehaviorRange(int resolution, double value)
    {
        int bin = (int)(value * resolution);

        if (bin >= resolution)
            bin = resolution - 1;

        return bin;
    }

    public static int EnemyDifficultyBehavior(MapInfo map)
    {
        if (map == null || map.components == null || map.components.Count == 0)
            return 0;

        float sumDensity = 0f;
        int counted = 0;

        foreach (var c in map.components)
        {
            int tiles = (c.tiles != null) ? c.tiles.Count : 0;
            if (tiles <= 0) continue;

            // counts components with 0 enemies too: density will be 0
            float density = c.enemiesCount / (float)tiles;
            sumDensity += density;
            counted++;
        }

        if (counted == 0)
            return 0;

        float avgDensity = sumDensity / counted;

        // 4 bins (tune thresholds)
        if (avgDensity < 0.005f) return 0;
        if (avgDensity < 0.015f) return 1;
        if (avgDensity < 0.030f) return 2;
        return 3;
    }

    // Not sure how it works, but uses composition ranking to reduce search space to 1000 for resolution 10, 126 for resolution 20
    public static int EnemyRoleCompositionBehavior(List<(Vector2Int placement, int type)> enemies, int resolution)
    {
        if (enemies == null || enemies.Count == 0)
            return 0;

        // Count amount of each (types 0..4)
        int[] counts = new int[5];
        int total = 0;

        foreach (var e in enemies)
        {
            int t = e.type;
            if (t >= 40 && t <= 44) t -= 40;
            if (t < 0 || t > 4) continue;
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

    public static int FurnishingBehaviorExploration(MapInfo map)
    {
        float lootCountOnMain = 0f;
        float lootCountOptional = 0f;

        foreach (var loot in map.furnishing)
        {
            // fuck spikes
            if (loot.type == 0 || loot.type == 1)
                continue;

            var c = MapGenerator.GetComponentForTile(map, loot.placement);
            if (c == null)
                continue;

            if (c.onMainPath) lootCountOnMain++;
            else lootCountOptional++;
        }

        float total = lootCountOnMain + lootCountOptional;

        // No relevant loot found
        if (total <= 0f)
            return 5;

        float optionalShare = lootCountOptional / total;

        int score;
        if (optionalShare >= 0.80f) score = 0;
        else if (optionalShare >= 0.60f) score = 1;
        else if (optionalShare >= 0.40f) score = 2;
        else if (optionalShare >= 0.20f) score = 3;
        else score = 4;

        return score;
    }

    public static int FurnishingBehaviorSafety(MapInfo map)
    {
        float healthCount = 0f;
        float powerCount = 0f;

        foreach (var loot in map.furnishing)
        {
            // keep your exclusions
            if (loot.type == 0 || loot.type == 1)
                continue;
            if (loot.type == 2) healthCount++;
            else if (loot.type == 3) powerCount++;
        }

        float total = healthCount + powerCount;

        // No relevant loot found
        if (total <= 0f)
            return 5;

        float powerShare = powerCount / total;

        int score;
        if (powerShare >= 0.80f) score = 0;
        else if (powerShare >= 0.60f) score = 1;
        else if (powerShare >= 0.40f) score = 2;
        else if (powerShare >= 0.20f) score = 3;
        else score = 4;

        return score;
    }

    /*
                         ⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢀⣀⣀⣤⣀⣀⡀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
                        ⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣠⠔⠊⠉⠀⠀⠀⠀⠀⠈⠉⠒⢤⡀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
                        ⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⡤⠋⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠈⠳⣄⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
                        ⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⡼⠁⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢸⠤⠔⠒⠒⠛⠘⠓⠒⠲⠦⢄⠀⠀⠀⠀
                        ⠀⠀⠀⠀⠀⠀⠀⠀⠀⢠⠃⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢀⢞⡴⠍⠊⠉⠑⠂⡇⠀⡠⠀⡀⠀⠀⠀⠀⠀⠀⠙⢆⠀⠀
                        ⠀⠀⠀⠀⠀⠀⠀⠀⠀⢸⠀⠀⠀⠀⠀⠀⠀⠀⢀⠤⠒⠉⢈⠈⠁⠶⣤⠖⢠⠇⠀⠀⠀⠇⣆⠀⠀⠀⠀⠀⠀⠈⢧⠀
                        ⠀⠀⠀⠀⠀⠀⠀⠀⠀⢸⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣀⡤⢫⠂⠀⠀⠀⠀⢼⠀⠀⠀⠀⣄⠘⢦⡀⠀⠀⠀⠀⠀⠈⡆
                        ⠀⠀⠀⠀⠀⠀⠀⠀⠀⢸⠀⠀⠀⠀⠠⢖⣭⡾⠒⠈⠁⠀⠀⠀⠀⠀⠀⠀⠈⢣⠀⠀⠀⠈⠓⢄⠱⠀⠀⠀⠀⠀⠀⡇
                        ⠀⠀⠀⠀⠀⠀⠀⠀⠀⡔⠀⠀⡴⠛⠲⡴⢳⠀⠀⠀⠀⢀⡀⠀⠀⠀⠀⠀⠀⠀⡇⠀⠀⠀⠀⠈⣃⠤⠀⠀⠀⠀⢀⡇
                        ⠀⠀⠀⠀⠀⠀⠀⠀⡰⠁⠀⠀⢑⡤⠀⠁⠸⡀⠀⢀⡔⠊⠓⠒⠤⢄⣀⣀⣀⡴⠃⠀⠀⠀⠀⠐⠫⢄⣒⠤⠔⠀⢸⠁
                        ⠀⠀⠀⠀⠀⠀⠀⡸⠁⠀⠀⠰⡁⠀⡀⠀⠀⢧⠀⠸⡄⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠈⠀⠀⠀⠀⣠⠃⠀
                        ⠀⠀⠀⠀⠀⠀⣰⠁⠀⠀⠀⠀⠈⠉⢱⡀⠀⠈⢣⣀⠈⠒⠢⠤⣄⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⡴⠃⠀⠀
                        ⠀⠀⠀⠀⠀⢰⠃⠀⠀⠀⠀⠀⠀⠀⠀⢣⠀⠀⠀⠈⠉⠑⠒⠊⠁⠙⠦⣄⡀⠀⠀⠀⠀⠀⠀⠀⠀⣠⠴⡎⠀⠀⠀⠀
                        ⠀⠀⠀⠀⢀⠇⠀⠀⠀⠀⠀⠀⠀⠀⠀⠈⡆⠀⠀⠀⠀⠀⠀⠀⠀⠀⣀⠀⠈⠉⠛⠒⠒⠒⠒⠚⠉⠀⠀⡇⠀⠀⠀⠀
                        ⠀⠀⠀⠀⣸⠀⠀⠀⠀⠀⠀⢠⠀⠀⠀⠀⠘⠢⣄⠀⠀⠀⠀⠀⠀⠸⠅⠙⠔⠒⠒⡄⠀⠀⠀⠀⠀⠀⠀⡁⠀⠀⠀⠀
                        ⠀⠀⢀⣀⡇⠀⠀⠀⠀⠀⠀⠈⢦⠀⠀⠀⠀⠀⠀⠉⠙⠒⠒⠒⠤⠖⠁⠀⠀⠠⡤⠃⠀⠀⠀⠀⠀⠀⠀⡇⠀⠀⠀⠀
                        ⠀⡎⠁⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠳⢤⣀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢀⣠⠔⢆⣀⠜⠀⠀⠀⠀⠀⠀⠀⠀⡇⠀⠀⠀⠀
                        ⠀⡳⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠉⠉⠉⠒⠒⠒⢺⠋⠉⠁⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢠⠁⠀⠀⠀⠀
                        ⢸⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢸⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⡘⠀⠀⠀⠀⠀
                         */
}