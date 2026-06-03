using UnityEngine;

namespace MagicVillageDash.World.Biomes
{
    /// <summary>Plays the ambient particles of whichever biome the player is currently inside.</summary>
    public sealed class BiomeAmbienceController : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private Transform player;
        [SerializeField] private Vector3 playerOffset = new(0, 0, 10); // where to sample the biome (relative to player)
        [SerializeField] private MonoBehaviour chunkQueryProvider; // IActiveChunkQuery
        [SerializeField] private BiomeAmbience[] ambiences;

        private IActiveChunkQuery chunkQuery;
        private BiomeDefinition currentBiome;
        private ChunkRoot currentChunk;

        private void Awake()
        {
            chunkQuery = chunkQueryProvider as IActiveChunkQuery;
            if (chunkQuery == null)
                Debug.LogError($"{nameof(BiomeAmbienceController)}: chunkQueryProvider must implement IActiveChunkQuery.");
        }

        private void Update()
        {
            if (chunkQuery == null || player == null) return;

            float z = player.position.z + playerOffset.z;

            // Fast path: still inside the same chunk → no scan, no work.
            if (currentChunk != null && currentChunk.gameObject.activeInHierarchy && Contains(currentChunk, z))
                return;

            var chunk = chunkQuery.GetChunkContaining(z);
            if (chunk == null) return;

            currentChunk = chunk;
            if (chunk.Biome != currentBiome) SwitchTo(chunk.Biome);
        }

        private void SwitchTo(BiomeDefinition biome)
        {
            currentBiome = biome;
            for (int i = 0; i < ambiences.Length; i++)
            {
                var a = ambiences[i];
                if (a == null) continue;
                if (a.Handles(biome)) a.Activate();
                else a.Deactivate();
            }
        }

        private static bool Contains(ChunkRoot c, float worldZ)
        {
            float start = c.transform.position.z;
            return worldZ >= start && worldZ < start + c.ChunkLength;
        }
    }
}
