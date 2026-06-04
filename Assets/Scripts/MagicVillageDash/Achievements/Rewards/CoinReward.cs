using UnityEngine;
using ErccDev.Foundation.Core.Achievements;
using MagicVillageDash.Score;

namespace MagicVillageDash.Achievements.Rewards
{
    /// <summary>
    /// Credits coins to the run counter. Pulls <see cref="ICoinCounter"/> from the shared context
    /// so the reward never references a concrete counter — works for achievements and for the
    /// collection (via CollectionRewardGranter) alike. Attach to a definition's <c>rewards[]</c>.
    /// </summary>
    [CreateAssetMenu(menuName = "MagicVillageDash/Rewards/Coin Reward")]
    public sealed class CoinReward : Reward
    {
        [SerializeField] private int amount = 25;

        public override void Grant(IAchievementContext context)
        {
            if (context != null && context.TryGet<ICoinCounter>(out var coins))
                coins.Add(amount);
        }
    }
}
