using System.Collections.Generic;
using UnityEngine;
using ErccDev.Foundation.Core.Collection;
using MagicVillageDash.Collections;
using MagicVillageDash.World;

namespace MagicVillageDash.Collectibles
{
    /// <summary>
    /// Rare per-chunk spawner for relics. Unlike the coin rail (dense), this drops at most one relic
    /// per chunk, gated by a spawn chance and a chunk cooldown, and only picks entries the player
    /// hasn't discovered yet — so the world stops spawning relics once the album is complete.
    /// </summary>
    public sealed class RelicRailFiller : MonoBehaviour, IChunkFiller
    {
        [Header("References")]
        [SerializeField] private RelicFactory factory;
        [SerializeField] private List<CollectionEntryDefinition> catalog = new();

        [Header("Spawn Tuning")]
        [SerializeField, Range(0f, 1f)] private float spawnChancePerChunk = 0.15f;
        [SerializeField] private int minChunksBetween = 2;

        [Header("Lane Geometry")]
        [SerializeField] private int   laneCount   = 3;
        [SerializeField] private float laneWidth   = 2.5f;
        [SerializeField] private float spawnHeight = 1f;

        private ICollectionService collection;
        private readonly List<CollectionEntryDefinition> _undiscovered = new();
        private int _chunksUntilEligible;

        void OnEnable()
        {
            if (!factory) factory = FindAnyObjectByType<RelicFactory>();
            collection ??= FindAnyObjectByType<CollectionManager>(FindObjectsInactive.Exclude);
        }

        public void FillChunk(ChunkRoot chunk)
        {
            if (!factory || collection == null || catalog.Count == 0) return;

            if (_chunksUntilEligible > 0) { _chunksUntilEligible--; return; }
            if (Random.value > spawnChancePerChunk) return;

            var entry = PickUndiscovered();
            if (entry == null) return;       // nothing left to find — stop spawning

            int   lane = Random.Range(0, laneCount);
            float x    = (lane - (laneCount - 1) * 0.5f) * laneWidth;
            float z    = chunk.transform.position.z + Random.Range(0.25f, 0.75f) * chunk.ChunkLength;

            var relic = factory.Spawn(new Vector3(x, spawnHeight, z), Quaternion.identity, chunk.transform);
            relic.Configure(entry.entryId);

            _chunksUntilEligible = minChunksBetween;
        }

        private CollectionEntryDefinition PickUndiscovered()
        {
            _undiscovered.Clear();
            for (int i = 0; i < catalog.Count; i++)
            {
                var e = catalog[i];
                if (e != null && !collection.IsDiscovered(e.entryId))
                    _undiscovered.Add(e);
            }
            return _undiscovered.Count > 0 ? _undiscovered[Random.Range(0, _undiscovered.Count)] : null;
        }
    }
}
