using UnityEngine;
using ErccDev.Foundation.Core.Achievements;
using ErccDev.Foundation.Core.Collection;

namespace MagicVillageDash.Achievements.Rewards
{
    /// <summary>
    /// Marks a collection entry as discovered — the achievement-side counterpart to a relic pickup.
    /// Drop this on an <c>AchievementDefinition.rewards[]</c> to unlock a den structure (or any
    /// collectible) when the achievement is earned. Pulls <see cref="ICollectionService"/> from the
    /// shared context, exactly like CoinReward pulls ICoinCounter, so it never references the concrete
    /// manager. <c>Discover</c> is idempotent and persists itself via the CollectionManager.
    /// </summary>
    [CreateAssetMenu(menuName = "MagicVillageDash/Rewards/Unlock Collection Entry")]
    public sealed class UnlockCollectionEntryReward : Reward
    {
        [Header("Collection")]
        [Tooltip("Entry id to discover. Falls back to rewardId when left empty.")]
        [SerializeField] private string entryId;

        public override void Grant(IAchievementContext context)
        {
            if (context == null || !context.TryGet<ICollectionService>(out var collection)) return;

            var id = string.IsNullOrEmpty(entryId) ? rewardId : entryId;
            collection.Discover(id);   // idempotent; persists + fires events via the manager
        }
    }
}
