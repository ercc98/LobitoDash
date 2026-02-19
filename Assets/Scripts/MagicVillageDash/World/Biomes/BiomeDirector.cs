using System.Collections.Generic;
using UnityEngine;

namespace MagicVillageDash.World.Biomes
{
    public sealed class BiomeDirector : MonoBehaviour, IBiomeDirector
    {
        [Header("Start")]
        [SerializeField] private BiomeDefinition startingBiome;

        [SerializeField] private ChunkFactoryInstaller installer;

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
            if (installer == null)
                Debug.LogError($"{nameof(BiomeDirector)}: installer is not assigned.");

            if (startingBiome == null)
                Debug.LogError($"{nameof(BiomeDirector)}: startingBiome is not assigned.");
            ResetRun();
        }

        public void ResetRun()
        {
            connectorQueue.Clear();

            current = startingBiome;
            remainingInBlock = current != null ? current.PickBlockSize() : 0;
            needsStartChunk = true;

            // NOTE: We intentionally do NOT clear prefabToInstance.
            // Pools should persist across runs unless you explicitly want to rebuild them.
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
                ChunkFactory start = Resolve(current.PickStartFactory());
                if (start != null) return start;
            }

            // 3) block ended → transition
            if (remainingInBlock <= 0)
            {
                TransitionToNextBiome();
                return GetNextFactory(out spawnObstacles);
            }

            // 4) normal biome chunk
            remainingInBlock--;

            ChunkFactory loop = Resolve(current.PickLoopFactory());
            if (loop == null)
                Debug.LogError($"{nameof(BiomeDirector)}: Biome '{current.name}' returned null loop factory.");

            return loop;
        }

        private void TransitionToNextBiome()
        {
            var transitions = current.transitions;
            if (transitions == null || transitions.Length == 0)
            {
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

            // enqueue connectors (instances)
            int connectorCount = picked.PickConnectorCount();
            for (int i = 0; i < connectorCount; i++)
            {
                ChunkFactory connector = Resolve(picked.PickConnectorFactory());
                if (connector != null)
                    connectorQueue.Enqueue(connector);
            }

            // switch biome
            current = picked.toBiome;
            remainingInBlock = current.PickBlockSize();
            needsStartChunk = true;
        }

        private BiomeTransitionOption PickWeightedTransition( BiomeTransitionOption[] options, BiomeDefinition from)
        {
            float total = 0f;

            // pass 1: avoid same biome if enabled
            for (int i = 0; i < options.Length; i++)
            {
                var o = options[i];
                if (o == null || o.toBiome == null || o.weight <= 0f) continue;
                if (avoidSameBiome && o.toBiome == from) continue;
                total += o.weight;
            }

            // fallback: allow same biome
            bool allowSame = false;
            if (total <= 0f)
            {
                allowSame = true;
                total = 0f;
                for (int i = 0; i < options.Length; i++)
                {
                    var o = options[i];
                    if (o == null || o.toBiome == null || o.weight <= 0f) continue;
                    total += o.weight;
                }
                if (total <= 0f) return null;
            }

            float r = Random.value * total;
            float acc = 0f;

            for (int i = 0; i < options.Length; i++)
            {
                var o = options[i];
                if (o == null || o.toBiome == null || o.weight <= 0f) continue;
                if (!allowSame && avoidSameBiome && o.toBiome == from) continue;

                acc += o.weight;
                if (r <= acc) return o;
            }

            return null;
        }

        private ChunkFactory Resolve(ChunkFactory prefab)
        {
            if (prefab == null) return null;
            if (installer == null)
            {
                Debug.LogError($"{nameof(BiomeDirector)}: installer is not assigned.");
                return null;
            }

            var inst = installer.Resolve(prefab);
            if (inst == null)
                Debug.LogError($"{nameof(BiomeDirector)}: No instance found for prefab '{prefab.name}'. Did you include its biome in installer?");
            return inst;
        }
    }
}
