using System;
using UnityEngine;
using ErccDev.Foundation.Core.Gameplay;
using ErccDev.Foundation.Core.Collection;
using MagicVillageDash.Audio;
using MagicVillageDash.Collections;
using MagicVillageDash.World;

namespace MagicVillageDash.Collectibles
{
    /// <summary>
    /// A rare relic pickup. Unlike coins, collecting it registers a one-time discovery in the
    /// Collection compendium (<see cref="ICollectionService.Discover"/>) — re-running over an
    /// already discovered entry grants nothing. The owning <see cref="RelicFactory"/> recycles it
    /// through the <see cref="Collected"/> event.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class RelicCollectible : MonoBehaviour, ICollectible
    {
        [Header("Relic")]
        [SerializeField] private string entryId;
        [SerializeField] private string playerTag = "Player";

        // cached service to avoid Find on every trigger
        private static ICollectionService cachedCollection;
        public ChunkRoot Owner { get; internal set; }

        /// <summary>Raised when the relic is collected. Factory listens and recycles.</summary>
        public event Action<RelicCollectible, GameObject> Collected;

        public int Value => 0;

        /// <summary>Assigns which collection entry this relic represents (set by the spawner).</summary>
        public void Configure(string id) => entryId = id;

        void Awake()
        {
            if (cachedCollection == null)
                cachedCollection = FindAnyObjectByType<CollectionManager>(FindObjectsInactive.Exclude);
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Sensor")) return;
            if (!other.CompareTag(playerTag)) return;
            Debug.Log($"[Relic] Triggered by {other.name} (tag: {other.tag})", this);
            TryCollect(other.gameObject);
            gameObject.SetActive(false);
        }

        public bool TryCollect(GameObject collector)
        {
            if (collector == null) return false;

            cachedCollection?.Discover(entryId);   // idempotent: true only on first discovery
            Collected?.Invoke(this, collector);

            AudioManager.Instance?.Play(SfxId.Coin);
            return true;
        }
    }
}
