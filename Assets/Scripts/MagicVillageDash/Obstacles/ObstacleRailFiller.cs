using UnityEngine;
using MagicVillageDash.World;

namespace MagicVillageDash.Obstacles
{
    public sealed class ObstacleRailFiller : MonoBehaviour, IChunkFiller
    {
        [Header("References")]
        [SerializeField] private ObstacleRailGenerator generator;                

        void OnEnable()
        {
            if (!generator) generator = FindAnyObjectByType<ObstacleRailGenerator>();
        }

        public void FillChunk(ChunkRoot chunk)
        {
            if (generator)
            {
                float startZ = chunk.transform.position.z;
                float endZ = startZ + Mathf.Max(0.01f, chunk.ChunkLength);
                // Hand the blocked-lane mask to the chunk so the coin pass can read it.
                var blocked = generator.FillRange(chunk.transform, chunk.ChunkLength, startZ, endZ);
                chunk.SetBlockedLanes(blocked);
            }

        }
    }
}
