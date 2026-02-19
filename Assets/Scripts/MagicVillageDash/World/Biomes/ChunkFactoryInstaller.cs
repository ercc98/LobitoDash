using System.Collections.Generic;
using UnityEngine;

namespace MagicVillageDash.World.Biomes
{
    public sealed class ChunkFactoryInstaller : MonoBehaviour
    {
        [Header("Biomes used in this scene")]
        [SerializeField] private BiomeDefinition[] biomes;

        [Header("Where instances live")]
        [SerializeField] private Transform factoriesRoot;

        // Prefab -> Instance
        private readonly Dictionary<ChunkFactory, ChunkFactory> map = new();

        public IReadOnlyDictionary<ChunkFactory, ChunkFactory> Map => map;

        private void Awake()
        {
            if (factoriesRoot == null) factoriesRoot = transform;
            Install();
        }

        private void Install()
        {
            map.Clear();

            HashSet<ChunkFactory> unique = new();

            // Collect prefabs from biomes
            if (biomes != null)
            {
                foreach (BiomeDefinition b in biomes)
                {
                    if (b == null) continue;

                    AddAll(unique, b.startFactories);
                    AddAll(unique, b.loopFactories);

                    if (b.transitions != null)
                    {
                        foreach (BiomeTransitionOption t in b.transitions)
                        {
                            if (t == null) continue;
                            AddAll(unique, t.connectorFactories);
                        }
                    }
                }
            }

            // Instantiate each factory prefab once
            foreach (ChunkFactory prefab in unique)
            {
                if (prefab == null) continue;

                ChunkFactory instance = Instantiate(prefab, factoriesRoot);
                instance.name = prefab.name;

                // Ensure pool is ready
                instance.WarmPool();

                map[prefab] = instance;
            }
        }

        private static void AddAll(HashSet<ChunkFactory> set, ChunkFactory[] arr)
        {
            if (arr == null) return;
            for (int i = 0; i < arr.Length; i++)
                if (arr[i] != null) set.Add(arr[i]);
        }

        public ChunkFactory Resolve(ChunkFactory prefab)
        {
            if (prefab == null) return null;
            return map.TryGetValue(prefab, out var inst) ? inst : null;
        }
    }
}
