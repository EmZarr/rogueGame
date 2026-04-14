using System;
using System.Collections.Generic;
using UnityEngine;

public static class FitnessAndBehaviorHelpers
{
    // Scores how well a value fits within a desired interval [min, max].
    //
    // Returns a value in range [0, 1]:
    // - 1.0 if value is inside the interval (perfect score)
    // - smoothly decreasing towards 0 the further it moves outside
    //
    // falloff controls how harshly values outside the interval are penalized:
    // - higher = sharper penalty
    // - lower = softer penalty
    public static float ScoreInterval(float value, float min, float max, float falloff = 1.5f)
    {
        // Safety: if any inputs are invalid, return worst score
        if (float.IsNaN(value) || float.IsInfinity(value) ||
            float.IsNaN(min) || float.IsNaN(max) ||
            float.IsInfinity(min) || float.IsInfinity(max))
            return 0f;

        // Ensure min <= max (swap if needed)
        if (max < min) (min, max) = (max, min);

        // If value is inside the desired range → perfect score
        if (value >= min && value <= max)
            return 1f;

        // Compute how far outside the range the value is
        // Normalize distance relative to the boundary value
        //
        // Example:
        // value < min → distance from min
        // value > max → distance from max
        float d = (value < min)
            ? (min - value) / Mathf.Max(min, 1e-6f)
            : (value - max) / Mathf.Max(max, 1e-6f);

        // Convert distance into a smooth penalty
        //
        // Formula: 1 / (1 + d^falloff)
        //
        // Behavior:
        // d = 0   → score = 1
        // d ↑     → score ↓ smoothly
        // never hits 0 exactly (nice for evolutionary algorithms)
        return 1f / (1f + Mathf.Pow(d, falloff));
    }

    // Maps a continuous value (in range [0, 1]) into a discrete bin index.
    //
    // Example:
    // resolution = 10 → bins: [0..9]
    // value = 0.2 → bin = 2
    //
    // Effectively:
    // [0.0–0.1) → 0
    // [0.1–0.2) → 1
    // ...
    // [0.9–1.0] → 9
    public static int GetBehaviorRange(int resolution, double value)
    {
        // Convert value to bin index by scaling to resolution
        int bin = (int)(value * resolution);

        // Clamp upper bound (handles value == 1.0 case)
        // Without this, value = 1.0 → bin = resolution (out of bounds)
        if (bin >= resolution)
            bin = resolution - 1;

        return bin;
    }

    // Scores how consistent a list of values is around a known average.
    //
    // Returns:
    // 1 = all values are identical
    // 0 = values are as spread out as theoretically possible for this average
    //
    // Assumes values are in range [0, 1].
    public static float GetConsistencyScore(List<float> values, float average)
    {
        if (values == null || values.Count <= 1)
            return 1f; // one value (or none) is perfectly consistent by definition

        float absoluteDeviationSum = 0f;

        foreach (float value in values)
        {
            absoluteDeviationSum += Mathf.Abs(value - average);
        }

        // Mean absolute deviation from the average
        float meanAbsoluteDeviation = absoluteDeviationSum / values.Count;

        // For values constrained to [0,1], the maximum possible average absolute deviation
        // for a given mean occurs when all values are pushed to the extremes 0 and 1.
        // That maximum is: 2 * average * (1 - average)
        float maxPossibleDeviation = 2f * average * (1f - average);

        // If average is exactly 0 or 1, then all valid values must also be 0 or 1,
        // so consistency is perfect.
        if (maxPossibleDeviation <= 1e-6f)
            return 1f;

        float inconsistency = meanAbsoluteDeviation / maxPossibleDeviation;

        // Clamp for safety, then invert so:
        // 0 inconsistency -> 1 consistency
        // max inconsistency -> 0 consistency
        return 1f - Mathf.Clamp01(inconsistency);
    }

}