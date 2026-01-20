using System.Collections.Generic;
using UnityEngine;

namespace MagicVillageDash.World.Biomes
{
    public sealed class BiomeDirector : MonoBehaviour, IBiomeDirector
    {
        [Header("Start")]
        [SerializeField] private BiomeDefinition startingBiome;

        [Header("Rules")]
        [SerializeField] private bool avoidSameBiome = true;
        [SerializeField] private bool noObstaclesInConnectors = true;

        private BiomeDefinition current;
        private int remainingInBlock;
        private bool needsStartChunk;

        private readonly Queue<ChunkFactory> connectorQueue = new();

        public BiomeDefinition CurrentBiome => current;

        private void Awake()
        {
            ResetRun();
        }

        public void ResetRun()
        {
            connectorQueue.Clear();

            current = startingBiome;
            remainingInBlock = current != null ? current.PickBlockSize() : 0;
            needsStartChunk = true;
        }

        public ChunkFactory GetNextFactory(out bool spawnObstacles)
        {
            spawnObstacles = true;

            if (current == null)
            {
                Debug.LogError($"{nameof(BiomeDirector)}: No starting biome set.");
                return null;
            }

            // 1) connector chunks first
            if (connectorQueue.Count > 0)
            {
                spawnObstacles = !noObstaclesInConnectors;
                return connectorQueue.Dequeue();
            }

            // 2) start chunk once per biome
            if (needsStartChunk)
            {
                needsStartChunk = false;
                var start = current.PickStartFactory();
                if (start != null)
                    return start;
            }

            // 3) block ended → transition
            if (remainingInBlock <= 0)
            {
                TransitionToNextBiome();
                return GetNextFactory(out spawnObstacles);
            }

            // 4) normal biome chunk
            remainingInBlock--;
            return current.PickLoopFactory();
        }

        private void TransitionToNextBiome()
        {
            var transitions = current.transitions;
            if (transitions == null || transitions.Length == 0)
            {
                // stay in same biome
                remainingInBlock = current.PickBlockSize();
                needsStartChunk = false;
                return;
            }

            var picked = PickWeightedTransition(transitions, current);
            if (picked == null || picked.toBiome == null)
            {
                remainingInBlock = current.PickBlockSize();
                needsStartChunk = false;
                return;
            }

            // enqueue connectors
            int connectorCount = picked.PickConnectorCount();
            for (int i = 0; i < connectorCount; i++)
            {
                var cf = picked.PickConnectorFactory();
                if (cf != null)
                    connectorQueue.Enqueue(cf);
            }

            // switch biome
            current = picked.toBiome;
            remainingInBlock = current.PickBlockSize();
            needsStartChunk = true;
        }

        private BiomeTransitionOption PickWeightedTransition(
            BiomeTransitionOption[] options,
            BiomeDefinition from)
        {
            float total = 0f;

            // first pass (respect avoidSameBiome)
            for (int i = 0; i < options.Length; i++)
            {
                var o = options[i];
                if (o == null || o.toBiome == null || o.weight <= 0f)
                    continue;

                if (avoidSameBiome && o.toBiome == from)
                    continue;

                total += o.weight;
            }

            // fallback: allow same biome
            if (total <= 0f)
            {
                for (int i = 0; i < options.Length; i++)
                {
                    var o = options[i];
                    if (o == null || o.toBiome == null || o.weight <= 0f)
                        continue;
                    total += o.weight;
                }

                if (total <= 0f) return null;

                float r2 = Random.value * total;
                float acc2 = 0f;

                for (int i = 0; i < options.Length; i++)
                {
                    var o = options[i];
                    if (o == null || o.toBiome == null || o.weight <= 0f)
                        continue;

                    acc2 += o.weight;
                    if (r2 <= acc2)
                        return o;
                }
            }

            // normal pick
            float r = Random.value * total;
            float acc = 0f;

            for (int i = 0; i < options.Length; i++)
            {
                var o = options[i];
                if (o == null || o.toBiome == null || o.weight <= 0f)
                    continue;

                if (avoidSameBiome && o.toBiome == from)
                    continue;

                acc += o.weight;
                if (r <= acc)
                    return o;
            }

            return null;
        }
    }
}
