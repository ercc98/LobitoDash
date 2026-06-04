using System;
using System.Collections.Generic;
using MagicVillageDash.Collectibles;
using MagicVillageDash.Obstacles;
using MagicVillageDash.World.Biomes;
using UnityEngine;

namespace MagicVillageDash.World
{
    public sealed class ChunkRoot : MonoBehaviour
    {
        
        internal readonly List<CoinCollectible> coins = new();
        internal readonly List<ObstacleHazard>  obstacles = new();
        internal readonly List<RelicCollectible> relics = new();
        private CoinFactory coinFactory;
        private ObstacleFactory obstacleFactory;
        private RelicFactory relicFactory;
        [SerializeField] private float chunkLength = 40f;
        [SerializeField] private bool canSpawnObstacles = true;
        [SerializeField] private bool canSpawnEnemies = true;

        public float ChunkLength
        {
            get => chunkLength;
            set => chunkLength = value;
        }

        public bool CanSpawnObstacles
        {
            get => canSpawnObstacles;
            private set => canSpawnObstacles = value;
        }

        public bool CanSpawnEnemies
        {
            get => canSpawnEnemies;
            private set => canSpawnEnemies = value;
        }
        public ChunkFactory OwnerFactory { get; internal set; }

        /// <summary>Biome this chunk was spawned for; set by ChunkSpawner.</summary>
        public BiomeDefinition Biome { get; internal set; }


        void Awake()
        {
            if(coinFactory == null) coinFactory = FindAnyObjectByType<CoinFactory>();
            if (obstacleFactory == null) obstacleFactory = FindAnyObjectByType<ObstacleFactory>();
            if (relicFactory == null) relicFactory = FindAnyObjectByType<RelicFactory>();
        }

        /// <summary>Called by ChunkSpawner right after spawning the chunk.</summary>
        public void InjectFactories(CoinFactory coinF, ObstacleFactory obsF, RelicFactory relicF = null)
        {
            coinFactory = coinF;
            obstacleFactory = obsF;
            if (relicF != null) relicFactory = relicF;
        }

        // --- Registry API (called by factories on spawn/recycle) ---
        public void Register(CoinCollectible c)   { if (c && !coins.Contains(c)) coins.Add(c); }
        public void Unregister(CoinCollectible c) { if (c) coins.Remove(c); }
        public void Register(ObstacleHazard h)    { if (h && !obstacles.Contains(h)) obstacles.Add(h); }
        public void Unregister(ObstacleHazard h) { if (h) obstacles.Remove(h); }
        public void Register(RelicCollectible r)   { if (r && !relics.Contains(r)) relics.Add(r); }
        public void Unregister(RelicCollectible r) { if (r) relics.Remove(r); }

        public void ResetForPool()
        {
            // Recycle any leftover residents without scanning transforms
            ResetCoinsForPool();
            ResetObstaclesForPool();
            ResetRelicsForPool();
        }

        public void ResetCoinsForPool()
        {            
            if (coinFactory != null)
            {
                for (int i = coins.Count - 1; i >= 0; i--)
                    if (coins[i]) coinFactory.Recycle(coins[i]);
            }
            coins.Clear();
        }

        public void ResetObstaclesForPool()
        {
            if (obstacleFactory != null)
            {
                for (int i = obstacles.Count - 1; i >= 0; i--)
                    if (obstacles[i]) obstacleFactory.Recycle(obstacles[i]);
            }
            obstacles.Clear();
        }

        public void ResetRelicsForPool()
        {
            if (relicFactory != null)
            {
                for (int i = relics.Count - 1; i >= 0; i--)
                    if (relics[i]) relicFactory.Recycle(relics[i]);
            }
            relics.Clear();
        }

        
    }
}
