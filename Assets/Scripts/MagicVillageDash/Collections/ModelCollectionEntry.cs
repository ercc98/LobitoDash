using UnityEngine;
using ErccDev.Foundation.Core.Collection;

namespace MagicVillageDash.Collections
{
    /// <summary>
    /// A collection entry whose payoff is a 3D model the player unlocks and keeps — e.g. a wolf bed
    /// or a pet dog. Extends Foundation's <see cref="CollectionEntryDefinition"/> (display data +
    /// optional rewards) with the prefab to show once the entry is discovered.
    ///
    /// Discovery/ownership is already persisted by the collection (CollectionProgressData); this just
    /// carries the visual. A den/gallery scene reads <see cref="ICollectionService.IsDiscovered"/> for
    /// each entry and instantiates <see cref="modelPrefab"/> for the ones the player owns.
    /// </summary>
    [CreateAssetMenu(menuName = "MagicVillageDash/Collection/Model Entry")]
    public sealed class ModelCollectionEntry : CollectionEntryDefinition
    {
        [Header("Unlocked Model")]
        public GameObject modelPrefab;   // shown in the den/gallery once discovered
    }
}
