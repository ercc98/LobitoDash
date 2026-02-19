using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace MagicVillageDash.World.Biomes
{
    [Serializable]
    public sealed class BiomeTransitionOption
    {
        public BiomeDefinition toBiome;
        [Min(0f)] public float weight = 1f;

        [Header("Connector (optional)")]
        public ChunkFactory[] connectorFactories;
        [Min(0)] public int minConnectorChunks = 1;
        [Min(0)] public int maxConnectorChunks = 1;

        public int PickConnectorCount()
        {
            if (connectorFactories == null || connectorFactories.Length == 0) return 0;
            var min = Mathf.Max(0, minConnectorChunks);
            var max = Mathf.Max(min, maxConnectorChunks);
            return Random.Range(min, max + 1);
        }

        public ChunkFactory PickConnectorFactory()
        {
            if (connectorFactories == null || connectorFactories.Length == 0) return null;
            return connectorFactories[Random.Range(0, connectorFactories.Length)];
        }
    }
}
