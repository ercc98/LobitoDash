using UnityEngine;
using ErccDev.Foundation.Core.Collection;
using MagicVillageDash.Data;

namespace MagicVillageDash.Collections
{
    /// <summary>
    /// Game-side collection engine. Subclasses Foundation's <see cref="CollectionManagerBase"/>
    /// (entries live in the SO definitions) and routes discovery progress through GameDataService's
    /// save list (playerdata.json) instead of the base's standalone "collection.json" file — same
    /// pattern as the achievement manager.
    ///
    /// The engine is reward-agnostic on purpose: first-discovery rewards are applied by a separate
    /// CollectionRewardGranter that observes <see cref="CollectionManagerBase.OnDiscovered"/>, so the
    /// manager never owns economy logic. Relics in the world call
    /// <see cref="CollectionManagerBase.Discover"/> on pickup; popups/SFX can hook the same event.
    /// </summary>
    public sealed class CollectionManager : CollectionManagerBase
    {
        // ---------- Persistence routed through the SO save system ----------

        protected override void LoadProgress()
        {
            if (progress == null) return;

            GameDataService._instance?.LoadAll();   // pull latest playerdata.json into the SOs
            progress.Invalidate();                  // rebuild the fast lookup from the loaded list
        }

        protected override void SaveProgress()
        {
            if (progress == null) return;

            // progress SO lives in GameDataService's BuildObjects list, so it rides the same file.
            GameDataService._instance?.SaveAll();
        }

        private void OnEnable()  => OnDiscovered += HandleDiscovered;
        private void OnDisable() => OnDiscovered -= HandleDiscovered;

        private void HandleDiscovered(CollectionEntryDefinition def)
        {
            // Base persists and fires "collectionEntryDiscovered" / "collectionCompleted" on the
            // EventBus; the reward granter handles rewards. Plug a toast/SFX here when the HUD lands.
            Debug.Log($"[Collection] Discovered: {def.title} ({def.entryId})", this);
        }
    }
}
