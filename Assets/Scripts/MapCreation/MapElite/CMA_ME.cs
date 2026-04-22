using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using System.Text;
using JetBrains.Annotations;
using UnityEditorInternal;
using System;

public class CMA_ME : MapElite
{
    protected override void Start()
    {
        RunMapElitesGeometry();
        MapJsonExporter.SaveMaps(geoArchive.Values.ToList(), "geoArchive_maps.json");

        RunMapElitesFurnishing();
        MapJsonExporter.SaveMaps(furnArchive.Values.ToList(), "furnArchive_maps.json");

        RunCMA_EMEnemies();
        MapJsonExporter.SaveMaps(enemArchive.Values.ToList(), "enemArchive_maps.json");
    }


    private void RunCMA_EMEnemies()
    {
        // Make 10 emitters, starting random places
        List<Emitter> emitters = new List<Emitter>();
        for (int i = 0; i < 10; i++) {
            emitters.Add(new Emitter(furnArchive));
        }

        for (int i = 0; i < totalIterations * 3; i++) {
            Emitter e = emitters
            .OrderBy(em => em.totalGenerated)
            .First();
            Map candidate = e.GenerateMap();
            var (fitness, behavior) = EnemFitAndBehav.GetEnemyFitnessAndBehavior(candidate);
            candidate.enemFitness = fitness;
            candidate.enemyBehavior = new Vector2Int(behavior.enemyType, behavior.difficulty);
            e.ReturnSolution(candidate, enemArchive);
        }

    }

    public class Emitter
    {
        // Current search center, our "mean"
        public Map referenceMap;

        // Learned mutation tendencies, our "Covariance Matrix" 
        public float[] compBias = new float[MapHelpers.EnemyTypes.Length];
        public float diffBias = 0.0f;

        // Successful maps from this batch. Discovered new bins or had higher fitness
        public List<SuccessfulSample> parents = new();

        // Total generated maps this batch
        public int sampledThisGeneration = 0;

        public int totalGenerated = 0;

        // Batch size before update / restart
        public const int lambda = 10;

        public Emitter(Dictionary<(Vector2, Vector2), Map> archive)
        {
            referenceMap = GenerateRandomEnemies(SelectRandom(archive).Clone());
        }

        public void ReturnSolution(Map map, Dictionary<(Vector2, Vector2, Vector2), Map> archive)
        {
            sampledThisGeneration++;
            totalGenerated++;

            bool discoveredNewCell = false;
            float fitnessDelta = 0f;

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
                archive[map.combinedBehavior] = map;
            }

            if (sampledThisGeneration >= lambda)
                UpdateEmitter(archive);
        }
        public void UpdateEmitter(Dictionary<(Vector2, Vector2, Vector2), Map> archive)
        {
            if (parents.Count > 0)
            {
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
                referenceMap = CMA_ME.SelectRandom(archive).Clone();

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
            // TODO, bias mutation with biases
            return MutateEnemies(referenceMap);
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
