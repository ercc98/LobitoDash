using UnityEngine;
using MagicVillageDash.World;

namespace MagicVillageDash.Collectibles
{

    public sealed class CoinRailFiller : MonoBehaviour, IChunkFiller
    {
        [Header("References")]
        [SerializeField] private CoinRailGenerator generator;

        void OnEnable()
        {
            if (!generator) generator = FindAnyObjectByType<CoinRailGenerator>();
        }

        public void FillChunk(ChunkRoot chunk)
        {
            if (generator)
            {
                float startZ = chunk.transform.position.z;
                float endZ = startZ + Mathf.Max(0.01f, chunk.ChunkLength);
                generator.FillRange(chunk.transform, startZ, endZ, chunk.BlockedLanes);
            }

        }

    }
}
