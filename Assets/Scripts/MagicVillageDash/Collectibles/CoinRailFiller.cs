using System.Collections.Generic;
using UnityEngine;
using MagicVillageDash.World;

namespace MagicVillageDash.Collectibles
{

    public sealed class CoinRailFiller : MonoBehaviour, IChunkFiller
    {
        [Header("References")]
        [SerializeField] private CoinRailGenerator generator;

        // Scratch list reused per chunk so predefined-segment fills don't allocate.
        readonly List<CoinSegment> _segments = new();

        void OnEnable()
        {
            if (!generator) generator = FindAnyObjectByType<CoinRailGenerator>();
        }

        public void FillChunk(ChunkRoot chunk)
        {
            if (!generator) return;

            // Predefined mode: if the chunk carries CoinSegment markers, only fill those Z-ranges.
            // Otherwise fall back to automatic mode and fill the whole chunk like before.
            chunk.GetComponentsInChildren(true, _segments);
            if (_segments.Count > 0)
                FillPredefinedSegments(chunk);
            else
                FillWholeChunk(chunk);
        }

        /// <summary>Automatic mode — the rail spans the entire chunk length.</summary>
        void FillWholeChunk(ChunkRoot chunk)
        {
            float startZ = chunk.transform.position.z;
            float endZ = startZ + Mathf.Max(0.01f, chunk.ChunkLength);
            generator.FillRange(chunk.transform, startZ, endZ, chunk.BlockedLanes);
        }

        /// <summary>Predefined mode — coins follow each marked segment's line (X and Z), in Z order.</summary>
        void FillPredefinedSegments(ChunkRoot chunk)
        {
            // Forward order so multiple segments lay down front-to-back.
            _segments.Sort((a, b) => a.MinZ.CompareTo(b.MinZ));
            for (int i = 0; i < _segments.Count; i++)
            {
                var seg = _segments[i];
                if (seg != null && seg.IsValid)
                    generator.FillLine(chunk.transform, seg.StartWorld, seg.EndWorld);
            }
        }

    }
}
