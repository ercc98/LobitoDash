using UnityEngine;
using TMPro;
using ErccDev.Foundation.Core.Achievements;
using ErccDev.Foundation.Core.Collection;
using MagicVillageDash.Achievements;
using MagicVillageDash.Collections;

namespace MagicVillageDash.UI.Gallery
{
    /// <summary>
    /// Drives the rewards gallery scene: spawns a <see cref="GalleryItemView"/> per entry into a grid so
    /// the player can browse everything the game offers and see what they've earned versus what's still
    /// locked. Read-only — it asks the live services (<see cref="IAchievementService"/> /
    /// <see cref="ICollectionService"/>) for ownership and never mutates progress.
    ///
    /// Two sections share one prefab: achievements (with live 0..1 progress for in-flight ones) and
    /// collection rewards (owned-or-not). Either catalog can be left empty to show just the other.
    /// </summary>
    public sealed class RewardsGalleryController : MonoBehaviour
    {
        [Header("Catalogs (the authored source of truth)")]
        [SerializeField] private AchievementCatalog achievementCatalog;
        [SerializeField] private CollectionCatalog collectionCatalog;

        [Header("Services (resolved at runtime if left empty)")]
        [SerializeField] private MonoBehaviour achievementServiceProvider;   // IAchievementService
        [SerializeField] private MonoBehaviour collectionServiceProvider;    // ICollectionService

        [Header("Layout")]
        [SerializeField] private GalleryItemView itemPrefab;
        [Tooltip("Parent with a GridLayoutGroup the cards are spawned under.")]
        [SerializeField] private Transform itemContainer;

        [Header("Summary (optional)")]
        [Tooltip("e.g. \"Earned 5 / 13\". Counts both sections together.")]
        [SerializeField] private TMP_Text summaryText;

        private IAchievementService _achievements;
        private ICollectionService  _collection;

        void Start()
        {
            _achievements = achievementServiceProvider as IAchievementService
                            ?? FindAnyObjectByType<AchievementManager>(FindObjectsInactive.Exclude);
            _collection   = collectionServiceProvider as ICollectionService
                            ?? FindAnyObjectByType<CollectionManager>(FindObjectsInactive.Exclude);
            Rebuild();
        }

        /// <summary>Clears and re-spawns every card from the current catalogs + live progress.</summary>
        [ContextMenu("Rebuild")]
        public void Rebuild()
        {
            if (itemPrefab == null || itemContainer == null) return;

            for (int i = itemContainer.childCount - 1; i >= 0; i--)
                Destroy(itemContainer.GetChild(i).gameObject);

            int total = 0, earned = 0;

            if (achievementCatalog != null)
                foreach (var def in achievementCatalog.Achievements)
                {
                    if (def == null) continue;
                    bool owned = _achievements != null && _achievements.IsUnlocked(def.achievementId);
                    float prog = _achievements != null ? _achievements.GetProgress01(def.achievementId) : -1f;

                    Spawn().Bind(def.icon, def.title, def.description, owned, def.hidden, prog);
                    total++; if (owned) earned++;
                }

            if (collectionCatalog != null)
                foreach (var def in collectionCatalog.Entries)
                {
                    if (def == null) continue;
                    bool owned = _collection != null && _collection.IsDiscovered(def.entryId);

                    Spawn().Bind(def.icon, def.title, def.description, owned, def.hidden);
                    total++; if (owned) earned++;
                }

            if (summaryText != null)
                summaryText.text = $"Earned {earned} / {total}";
        }

        private GalleryItemView Spawn() => Instantiate(itemPrefab, itemContainer);
    }
}
