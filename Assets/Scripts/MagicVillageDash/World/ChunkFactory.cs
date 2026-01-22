using UnityEngine;
using ErccDev.Foundation.Core.Factories;
using System;

namespace MagicVillageDash.World
{
    [Serializable]
    public sealed class ChunkFactory : Factory<ChunkRoot>
    {
        public override ChunkRoot Spawn(Vector3 position, Quaternion rotation, Transform parent = null)
        {
            var chunk = base.Spawn(position, rotation, parent.transform);
            return chunk;
        }

        public override void Recycle(ChunkRoot instance)
        {
            if (instance) instance.ResetForPool();
            base.Recycle(instance);
        }
    }
}
