using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using System.Text;
using JetBrains.Annotations;
using System.Collections.Concurrent;
//using UnityEditorInternal;
using System;
//using static UnityEditor.Progress;

public class CMA_ME : MapElite
{
    protected override void Start()
    {
        RunMapElitesGeometry();
        MapJsonExporter.SaveMaps(geoArchive.Values.ToList(), "geoArchiveEasy_maps.json");

        RunMapElitesFurnishing();
        MapJsonExporter.SaveMaps(furnArchive.Values.ToList(), "furnArchiveEasy_maps.json");

        RunCMA_EMEnemies();
        MapJsonExporter.SaveMaps(enemArchive.Values.ToList(), "enemArchiveEasy_maps.json");
    }


    private void RunCMA_EMEnemies()
    {
        int completedEnem = 0;

        List<Emitter> emitters = new List<Emitter>();
        for (int i = 0; i < 15; i++)
            emitters.Add(new Emitter(furnArchive));

        var safeArchive = new ConcurrentDictionary<(Vector2, Vector2, Vector2), Map>();

        // Random phase - same as before but writing to safeArchive
        for (int i = 0; i < initialRandomSolutions; i++)
        {
            Map candidate = GenerateRandomEnemies(SelectRandom(furnArchive).Clone());
            var (fitness, behavior) = EnemFitAndBehav.GetEnemyFitnessAndBehavior(candidate);
            candidate.enemFitness = fitness;
            candidate.enemyBehavior = new Vector2Int(behavior.enemyType, behavior.difficulty);

            safeArchive.AddOrUpdate(
                candidate.combinedBehavior,
                candidate,
                (key, existing) => existing.enemFitness < candidate.enemFitness ? candidate : existing
            );
        }

        // Emitter phase - now parallel
        int perEmitter = (totalIterations * 3 - initialRandomSolutions) / emitters.Count;

        var options = new System.Threading.Tasks.ParallelOptions
        {
            MaxDegreeOfParallelism = System.Environment.ProcessorCount - 2 // leave some cores free
        };

        System.Threading.Tasks.Parallel.ForEach(emitters, options, e =>
        {
            for (int i = 0; i < perEmitter; i++)
            {
                Map candidate = e.GenerateMap();
                var (fitness, behavior) = EnemFitAndBehav.GetEnemyFitnessAndBehavior(candidate);
                candidate.enemFitness = fitness;
                candidate.enemyBehavior = new Vector2Int(behavior.enemyType, behavior.difficulty);
                e.ReturnSolution(candidate, safeArchive);

                int completed = System.Threading.Interlocked.Increment(ref completedEnem);
                if (trainingLogger != null && completed % trainingLogger.LogEveryNIterations == 0)
                    trainingLogger.LogEnemies(completed, new Dictionary<(Vector2, Vector2, Vector2), Map>(safeArchive));
            }
        });
        //trainingLogger?.LogEnemies(totalIterations * 3, enemArchive);

        // Copy back to enemArchive if needed elsewhere
        foreach (var kvp in safeArchive)
            enemArchive[kvp.Key] = kvp.Value;

        foreach (Emitter emitter in emitters)
            Debug.Log("Restarts: " + emitter.restartAmount + " out of " + emitter.nonRestart);
    }

    public class Emitter
    {
        // Current search center, our "mean"
        public Map referenceMap;
        public int restartAmount = 0;
        public int nonRestart = 0;

        // Learned mutation tendencies, our "Covariance Matrix" 
        public float[] compBias = new float[MapHelpers.EnemyTypes.Length];
        public float diffBias = 0.0f;

        // Successful maps from this batch. Discovered new bins or had higher fitness
        public List<SuccessfulSample> parents = new();

        // Total generated maps this batch
        public int sampledThisGeneration = 0;

        public int totalGenerated = 0;

        // Batch size before update / restart
        public const int lambda = 37;

        public Emitter(Dictionary<(Vector2, Vector2), Map> archive)
        {
            referenceMap = GenerateRandomEnemies(SelectRandom(archive).Clone());
        }

        public void ReturnSolution(Map map, ConcurrentDictionary<(Vector2, Vector2, Vector2), Map> archive)
        {
            sampledThisGeneration++;
            totalGenerated++;

            bool discoveredNewCell = false;
            float fitnessDelta = 0f;

            /*
            // New cell
            if (!archive.ContainsKey(map.combinedBehavior))
            {
                discoveredNewCell = true;
                fitnessDelta = map.enemFitness;
            }
            // Improved old
            else if (archive[map.combinedBehavior].enemFitness < map.enemFitness)
            {
                fitnessDelta = map.enemFitness - archive[map.combinedBehavior].enemFitness;
            }*/
            if (archive.TryGetValue(map.combinedBehavior, out Map existing))
            {
                if (existing.enemFitness < map.enemFitness)
                    fitnessDelta = map.enemFitness - existing.enemFitness;
            }
            else
            {
                discoveredNewCell = true;
                fitnessDelta = map.enemFitness;
            }

            if (discoveredNewCell || fitnessDelta > 0f)
            {
                var compDelta = new float[MapHelpers.EnemyTypes.Length];
                for (int i = 0; i < compBias.Length; i++)
                {
                    compDelta[i] = map.enemyComp[i] - referenceMap.enemyComp[i];
                }

                float diffDelta = map.difficulty - referenceMap.difficulty;

                parents.Add(new SuccessfulSample(
                    map,
                    fitnessDelta,
                    discoveredNewCell,
                    compDelta,
                    diffDelta
                ));
                archive.AddOrUpdate(
                    map.combinedBehavior,
                    map,
                    (key, existing) => existing.enemFitness < map.enemFitness ? map : existing
);
            }

            if (sampledThisGeneration >= lambda)
                UpdateEmitter(archive);
        }
        // Change signature from Dictionary to ConcurrentDictionary:
        public void UpdateEmitter(ConcurrentDictionary<(Vector2, Vector2, Vector2), Map> archive)
        {
            if (parents.Count > 0)
            {
                nonRestart++;
                // Improvement emitter ordering:
                // 1. new cells first
                // 2. larger fitness delta after
                parents = parents
                    .OrderByDescending(p => p.discoveredNewCell)
                    .ThenByDescending(p => p.fitnessDelta)
                    .ToList();

                // 1. Update reference map: best successful child becomes new center
                referenceMap = parents[0].map.Clone();

                // 2. Update enemy composition bias: average successful composition delta
                Array.Clear(compBias, 0, compBias.Length);

                for (int i = 0; i < parents.Count; i++)
                {
                    float weight = GetParentWeight(i);

                    for (int j = 0; j < compBias.Length; j++)
                    {
                        compBias[j] += parents[i].compDelta[j] * weight;
                    }
                }
                NormalizeCompBias(compBias);

                // 3. Update difficulty bias: weighted average successful difficulty delta
                diffBias = 0.0f;
                for (int i = 0; i < parents.Count; i++)
                {
                    float weight = GetParentWeight(i);
                    diffBias += parents[i].diffDelta * weight;
                }
                // clamp to avoid huge jumps
                diffBias = Mathf.Clamp(diffBias, -1f, 1f);

                // Reset batch state
                parents.Clear();
                sampledThisGeneration = 0;
            }
            else
            {
                // No successful children in this batch -> restart
                restartAmount++;
                referenceMap = CMA_ME.SelectRandom(new Dictionary<(Vector2, Vector2, Vector2), Map>(archive)).Clone();

                compBias = new float[MapHelpers.EnemyTypes.Length];
                diffBias = 0.0f;

                parents.Clear();
                sampledThisGeneration = 0;
                return;
            }
        }

        private float GetParentWeight(int sortedIndex)
        {
            // Simplest option: equal weights
            return 1f / parents.Count;

            // Alternative if we want top samples to matter more:
            // float raw = parents.Count - sortedIndex;
            // float denom = parents.Count * (parents.Count + 1) / 2f;
            // return raw / denom;
        }

        private void NormalizeCompBias(float[] bias)
        {
            // subtract mean so the bias sums roughly to 0
            // (more of some types, less of others)
            float sum = 0f;
            for (int i = 0; i < bias.Length; i++)
                sum += bias[i];

            float mean = sum / bias.Length;
            for (int i = 0; i < bias.Length; i++)
                bias[i] -= mean;

            // clamp to avoid huge jumps
            for (int i = 0; i < bias.Length; i++)
                bias[i] = Mathf.Clamp(bias[i], -1f, 1f);
        }

        public Map GenerateMap()
        {
            return ObjectPlacementGenerator.BiasedMutateEnemies(referenceMap.Clone(), compBias, diffBias);
        }
    }

    // Successful map/sample. Had higher fitness or new behavior
    public class SuccessfulSample
    {
        public Map map; 
        public float fitnessDelta;
        public bool discoveredNewCell;
        // child composition - referenceMap composition
        public float[] compDelta;
        // child difficulty - referenceMap difficulty
        public float diffDelta;

        public SuccessfulSample(Map map, float fitnessDelta, bool discoveredNewCell, float[] compDelta, float diffDelta)
        {
            this.map = map;
            this.fitnessDelta = fitnessDelta;
            this.discoveredNewCell = discoveredNewCell;
            this.compDelta = compDelta;
            this.diffDelta = diffDelta;

        }
    }
}
