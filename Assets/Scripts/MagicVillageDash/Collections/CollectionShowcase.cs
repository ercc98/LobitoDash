using System.Collections.Generic;
using UnityEngine;
using ErccDev.Foundation.Core.Collection;

namespace MagicVillageDash.Collections
{
    /// <summary>
    /// Den / gallery display: spawns the 3D models the player has unlocked. For each entry in the
    /// catalog it asks the <see cref="ICollectionService"/> whether it's discovered and, if so,
    /// instantiates the entry's <see cref="ModelCollectionEntry.modelPrefab"/> at the matching slot.
    /// Read-only — ownership/persistence already live in the collection + GameDataService.
    /// </summary>
    public sealed class CollectionShowcase : MonoBehaviour
    {
        [Header("Source")]
        [SerializeField] private MonoBehaviour collectionServiceProvider;   // ICollectionService (the CollectionManager)
        [SerializeField] private List<ModelCollectionEntry> catalog = new();

        [Header("Placement")]
        [Tooltip("One anchor per catalog entry (index-matched). Falls back to this transform if missing.")]
        [SerializeField] private List<Transform> slots = new();

        [Tooltip("Also spawn a placeholder for not-yet-unlocked entries (e.g. a locked silhouette).")]
        [SerializeField] private bool showLockedPlaceholders = false;
        [SerializeField] private GameObject lockedPlaceholderPrefab;

        private ICollectionService _collection;
        private readonly List<GameObject> _spawned = new();

        void Start()
        {
            _collection = collectionServiceProvider as ICollectionService
                          ?? FindAnyObjectByType<CollectionManager>(FindObjectsInactive.Exclude);
            Rebuild();
        }

        /// <summary>Clears and re-spawns the showcase from current progress.</summary>
        [ContextMenu("Rebuild")]
        public void Rebuild()
        {
            Clear();
            if (_collection == null) return;

            for (int i = 0; i < catalog.Count; i++)
            {
                var entry = catalog[i];
                if (entry == null) continue;

                bool owned   = _collection.IsDiscovered(entry.entryId);
                var  prefab  = owned ? entry.modelPrefab
                              : showLockedPlaceholders ? lockedPlaceholderPrefab : null;
                if (prefab == null) continue;

                var slot = i < slots.Count && slots[i] ? slots[i] : transform;
                _spawned.Add(Instantiate(prefab, slot.position, slot.rotation, slot));
            }
        }

        public void Clear()
        {
            for (int i = _spawned.Count - 1; i >= 0; i--)
                if (_spawned[i]) Destroy(_spawned[i]);
            _spawned.Clear();
        }
    }
}
