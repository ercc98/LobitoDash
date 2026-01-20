using UnityEngine;

namespace MagicVillageDash.World.Biomes
{
    [CreateAssetMenu(fileName = "BiomeDefinition", menuName = "MagicVillageDash/Biomes/Biome Definition")]
    public sealed class BiomeDefinition : ScriptableObject
    {
        [Header("Id")]
        public string biomeId = "Forest";

        [Header("Chunk Factories")]
        public ChunkFactory[] startFactories;
        public ChunkFactory[] loopFactories;

        [Header("Block Length (chunks)")]
        [Min(1)] public int minBlockChunks = 6;
        [Min(1)] public int maxBlockChunks = 10;

        [Header("Markov Transitions (outgoing)")]
        [Tooltip("Weighted list of possible next biomes when the block ends.")]
        public BiomeTransitionOption[] transitions;

        public int PickBlockSize()
        {
            var min = Mathf.Max(1, minBlockChunks);
            var max = Mathf.Max(min, maxBlockChunks);
            return Random.Range(min, max + 1);
        }

        public ChunkFactory PickStartFactory()
        {
            if (startFactories == null || startFactories.Length == 0) return null;
            return startFactories[Random.Range(0, startFactories.Length)];
        }

        public ChunkFactory PickLoopFactory()
        {
            if (loopFactories == null || loopFactories.Length == 0) return null;
            return loopFactories[Random.Range(0, loopFactories.Length)];
        }
    }
}