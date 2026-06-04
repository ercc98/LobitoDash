using UnityEngine;
using ErccDev.Foundation.Core.Factories;
using MagicVillageDash.World;

namespace MagicVillageDash.Collectibles
{
    /// <summary>
    /// Spawns pooled relics. When a relic is collected, the factory automatically recycles it.
    /// Relics are parented under the provided chunk so they move with the world.
    /// </summary>
    public sealed class RelicFactory : Factory<RelicCollectible>
    {
        public override RelicCollectible Spawn(Vector3 position, Quaternion rotation, Transform parent = null)
        {
            var relic = base.Spawn(position, rotation);
            relic.transform.SetParent(parent, true);
            var chunk = parent.GetComponentInParent<ChunkRoot>();
            relic.Owner = chunk;
            if (chunk) chunk.Register(relic);

            relic.Collected += HandleCollected;
            return relic;
        }

        public override void Recycle(RelicCollectible instance)
        {
            if (!instance) return;
            instance.Collected -= HandleCollected;

            var chunk = instance.Owner;
            if (chunk) chunk.Unregister(instance);
            instance.Owner = null;

            instance.transform.SetParent(transform, false);
            base.Recycle(instance);
        }

        void HandleCollected(RelicCollectible relic, GameObject _) => Recycle(relic);
    }
}
