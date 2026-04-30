using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class MapSelector : MonoBehaviour
{
    [SerializeField] private string inputFileName = "enemArchiveEasy_maps.json";
    [SerializeField] private string outputFileName = "diverse_maps.json";

    private HashSet<int> geoUsed = new HashSet<int>();
    private HashSet<int> enemyCompUsed = new HashSet<int>();

    private struct ClosestPair
    {
        public int indexA;
        public int indexB;
        public float distance;
    }


    void Start()
    {
        string path = Path.Combine(Application.streamingAssetsPath, inputFileName);
        string archiveToStoreIn = Path.Combine(Application.streamingAssetsPath, outputFileName);

        var maps = MapJsonExporter.LoadMaps(path);
        var selectedMaps = SelectMaps(maps, 6);

        MapJsonExporter.SaveMaps(selectedMaps, archiveToStoreIn);

    }

    public static List<Map> SelectMaps(
        IReadOnlyList<Map> maps, int amount = 6)
    {
        List<Map> selectedMaps = new List<Map>(amount);

        for (int i = 0; i < amount; i++)
            selectedMaps.Add(maps[i]);

        ClosestPair currentClosest = FindClosestPair(selectedMaps);

        bool improved = true;

        while (improved)
        {
            improved = false;

            float bestDistance = currentClosest.distance;
            int bestRemoveIndex = -1;
            Map bestAddMap = null;
            ClosestPair bestClosestAfterSwap = currentClosest;

            for (int candidateIndex = 0; candidateIndex < maps.Count; candidateIndex++)
            {
                Map candidate = maps[candidateIndex];

                if (selectedMaps.Contains(candidate))
                    continue;

                for (int removeIndex = 0; removeIndex < selectedMaps.Count; removeIndex++)
                {
                    Map removed = selectedMaps[removeIndex];

                    selectedMaps[removeIndex] = candidate;

                    ClosestPair testClosest = FindClosestPair(selectedMaps);

                    selectedMaps[removeIndex] = removed;

                    if (testClosest.distance > bestDistance)
                    {
                        bestDistance = testClosest.distance;
                        bestRemoveIndex = removeIndex;
                        bestAddMap = candidate;
                        bestClosestAfterSwap = testClosest;
                    }
                }
            }

            if (bestAddMap != null)
            {
                selectedMaps[bestRemoveIndex] = bestAddMap;
                currentClosest = bestClosestAfterSwap;
                improved = true;
            }
        }

        return selectedMaps;

    }

    private static ClosestPair FindClosestPair(List<Map> selectedMaps)
    {
        ClosestPair closest = new ClosestPair
        {
            indexA = -1,
            indexB = -1,
            distance = float.PositiveInfinity
        };

        for (int i = 0; i < selectedMaps.Count; i++)
        {
            for (int j = i + 1; j < selectedMaps.Count; j++)
            {
                float distance = MapDistance(selectedMaps[i], selectedMaps[j]);

                if (distance < closest.distance)
                {
                    closest.indexA = i;
                    closest.indexB = j;
                    closest.distance = distance;
                }
            }
        }

        return closest;
    }

    // Enemy does count for a bit more
    private static float MapDistance(Map a, Map b)
    {
        float geoDistance = GeoDistance(a, b);
        float furnDistance = FurnDistance(a, b);
        float enemyDistance = EnemyCompDistance(a.enemyComp, b.enemyComp);

        return Mathf.Sqrt(
            geoDistance * geoDistance +
            furnDistance * furnDistance +
            enemyDistance * enemyDistance
        );
    }

    private static float GeoDistance(Map a, Map b)
    {
        // Ordered behavior distance.
        // 0 vs 12 is farther than 0 vs 6.
        return Mathf.Abs(a.geoBehavior.x - b.geoBehavior.x) / 12f;
    }

    private static float FurnDistance(Map a, Map b)
    {
        float dx = a.furnBehavior.x - b.furnBehavior.x;
        float dy = a.furnBehavior.y - b.furnBehavior.y;

        // furnBehavior values are 0..2 on both axes.
        return Mathf.Sqrt(dx * dx + dy * dy) / Mathf.Sqrt(8f);
    }


    private static float EnemyCompDistance(float[] a, float[] b)
    {
        float identityDistance = EnemyIdentityDistance(a, b);
        float shapeDistance = EnemyShapeDistance(a, b);

        return Mathf.Sqrt(
            identityDistance * identityDistance +
            shapeDistance * shapeDistance
        );
    }

    private static float EnemyIdentityDistance(float[] a, float[] b)
    {
        if (a == null || b == null)
            return 0f;

        int length = Mathf.Min(a.Length, b.Length);

        if (length == 0)
            return 0f;

        float totalA = 0f;
        float totalB = 0f;

        for (int i = 0; i < length; i++)
        {
            totalA += Mathf.Max(0f, a[i]);
            totalB += Mathf.Max(0f, b[i]);
        }

        if (totalA <= 0f || totalB <= 0f)
            return 0f;

        float shared = 0f;

        for (int i = 0; i < length; i++)
        {
            float pa = Mathf.Max(0f, a[i]) / totalA;
            float pb = Mathf.Max(0f, b[i]) / totalB;

            shared += Mathf.Min(pa, pb);
        }

        return 1f - shared;
    }
    private static float EnemyShapeDistance(float[] a, float[] b)
    {
        float effectiveA = EffectiveEnemyCount(a);
        float effectiveB = EffectiveEnemyCount(b);

        if (effectiveA <= 0f || effectiveB <= 0f)
            return 0f;

        int enemyTypeCount = Mathf.Min(a.Length, b.Length);

        if (enemyTypeCount <= 1)
            return 0f;

        float logA = Mathf.Log(effectiveA);
        float logB = Mathf.Log(effectiveB);

        return Mathf.Abs(logA - logB) / Mathf.Log(enemyTypeCount);
    }

    private static float EffectiveEnemyCount(float[] comp)
    {
        if (comp == null || comp.Length == 0)
            return 0f;

        float total = 0f;

        for (int i = 0; i < comp.Length; i++)
            total += Mathf.Max(0f, comp[i]);

        if (total <= 0f)
            return 0f;

        float entropy = 0f;

        for (int i = 0; i < comp.Length; i++)
        {
            float p = Mathf.Max(0f, comp[i]) / total;

            if (p > 0f)
                entropy -= p * Mathf.Log(p);
        }

        return Mathf.Exp(entropy);
    }
}