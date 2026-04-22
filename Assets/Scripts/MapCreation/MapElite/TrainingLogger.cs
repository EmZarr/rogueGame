using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

public class TrainingLogger : MonoBehaviour
{
    [Header("Logging")]
    [SerializeField] private bool enableLogging = true;
    [SerializeField] private int logEveryNIterations = 5000;
    [SerializeField] private string fileName = "training_log.csv";

    [Header("Behavior Bin Counts")]
    [SerializeField] private int geoBehaviorBins = 10;

    [SerializeField] private int furnLootBins = 3;
    [SerializeField] private int furnObstacleBins = 3;

    [SerializeField] private int enemyTypeBins = 126;
    [SerializeField] private int enemyDifficultyBins = 3;

    [SerializeField] private float fitnessThreshold = 0.8f;

    private string filePath;

    public int LogEveryNIterations => logEveryNIterations;

    private void Awake()
    {
        filePath = Path.Combine(Application.dataPath, fileName);
        InitializeFile();
    }

    private void InitializeFile()
    {
        if (!enableLogging) return;

        var sb = new StringBuilder();
        sb.AppendLine(
            "Stage,Iteration,ArchiveSize,AvgFitness,ElitesAboveThreshold,ArchiveCoveragePct," +
            "FilledBehavior1,MissingBehavior1," +
            "FilledBehavior2,MissingBehavior2," +
            "FilledBehavior3,MissingBehavior3"
        );

        File.WriteAllText(filePath, sb.ToString());
        Debug.Log($"Training log initialized: {filePath}");
    }

    public void LogGeometry(int iteration, Dictionary<Vector2, Map> archive)
    {
        if (!enableLogging) return;
        if (!ShouldLog(iteration)) return;

        var maps = archive.Values.ToList();
        if (maps.Count == 0) return;

        float avgFitness = maps.Average(m => m.geoFitness);
        int elitesAbove = maps.Count(m => m.geoFitness > fitnessThreshold);

        float archiveCoverage = (float)archive.Count / geoBehaviorBins * 100f;

        var filledGeo = maps
            .Select(m => m.geoBehavior.x)
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        var missingGeo = GetMissingBins(filledGeo, geoBehaviorBins);

        AppendRow(
            "Geometry",
            iteration,
            archive.Count,
            avgFitness,
            elitesAbove,
            archiveCoverage,
            ToPipeSeparated(filledGeo),
            ToPipeSeparated(missingGeo),
            "",
            "",
            "",
            ""
        );
    }

    public void LogFurnishing(int iteration, Dictionary<(Vector2, Vector2), Map> archive)
    {
        if (!enableLogging) return;
        if (!ShouldLog(iteration)) return;

        var maps = archive.Values.ToList();
        if (maps.Count == 0) return;

        float avgFitness = maps.Average(m => m.furnFitness);
        int elitesAbove = maps.Count(m => m.furnFitness > fitnessThreshold);

        float archiveCoverage = (float)archive.Count / (furnLootBins * furnObstacleBins) * 100f;

        var filledLoot = maps
            .Select(m => m.furnBehavior.x)
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        var filledObstacle = maps
            .Select(m => m.furnBehavior.y)
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        var missingLoot = GetMissingBins(filledLoot, furnLootBins);
        var missingObstacle = GetMissingBins(filledObstacle, furnObstacleBins);

        AppendRow(
            "Furnishing",
            iteration,
            archive.Count,
            avgFitness,
            elitesAbove,
            archiveCoverage,
            ToPipeSeparated(filledLoot),
            ToPipeSeparated(missingLoot),
            ToPipeSeparated(filledObstacle),
            ToPipeSeparated(missingObstacle),
            "",
            ""
        );
    }

    public void LogEnemies(int iteration, Dictionary<(Vector2, Vector2, Vector2), Map> archive)
    {
        if (!enableLogging) return;
        if (!ShouldLog(iteration)) return;

        var maps = archive.Values.ToList();
        if (maps.Count == 0) return;

        float avgFitness = maps.Average(m => m.enemFitness);
        int elitesAbove = maps.Count(m => m.enemFitness > fitnessThreshold);

        float archiveCoverage =
            (float)archive.Count /
            (geoBehaviorBins * furnLootBins * furnObstacleBins * enemyTypeBins * enemyDifficultyBins) * 100f;

        var filledEnemyTypes = maps
            .Select(m => m.enemyBehavior.x)
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        var filledDifficulties = maps
            .Select(m => m.enemyBehavior.y)
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        var missingEnemyTypes = GetMissingBins(filledEnemyTypes, enemyTypeBins);
        var missingDifficulties = GetMissingBins(filledDifficulties, enemyDifficultyBins);

        AppendRow(
            "Enemies",
            iteration,
            archive.Count,
            avgFitness,
            elitesAbove,
            archiveCoverage,
            ToPipeSeparated(filledEnemyTypes),
            ToPipeSeparated(missingEnemyTypes),
            ToPipeSeparated(filledDifficulties),
            ToPipeSeparated(missingDifficulties),
            "",
            ""
        );
    }

    private bool ShouldLog(int iteration)
    {
        return iteration > 0 && iteration % logEveryNIterations == 0;
    }

    // Assumes bins are 1-based: 1..binCount
    private List<int> GetMissingBins(List<int> filledBins, int binCount)
    {
        HashSet<int> filledSet = new HashSet<int>(filledBins);

        return Enumerable.Range(0, binCount)
            .Where(bin => !filledSet.Contains(bin))
            .ToList();
    }

    private string ToPipeSeparated(List<int> values)
    {
        return string.Join("|", values);
    }

    private void AppendRow(
        string stage,
        int iteration,
        int archiveSize,
        float avgFitness,
        int elitesAboveThreshold,
        float archiveCoveragePct,
        string filledBehavior1,
        string missingBehavior1,
        string filledBehavior2,
        string missingBehavior2,
        string filledBehavior3,
        string missingBehavior3)
    {
        string line =
            $"{stage},{iteration},{archiveSize}," +
            $"{avgFitness:F4},{elitesAboveThreshold},{archiveCoveragePct:F2}," +
            $"\"{filledBehavior1}\",\"{missingBehavior1}\"," +
            $"\"{filledBehavior2}\",\"{missingBehavior2}\"," +
            $"\"{filledBehavior3}\",\"{missingBehavior3}\"";

        File.AppendAllText(filePath, line + Environment.NewLine);
    }
}