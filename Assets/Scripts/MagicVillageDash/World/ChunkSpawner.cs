using UnityEngine;
using System.Collections.Generic;
using MagicVillageDash.Collectibles;
using MagicVillageDash.Obstacles;
using MagicVillageDash.World.Biomes;
using System;


namespace MagicVillageDash.World
{
    public sealed class ChunkSpawner : MonoBehaviour, IChunkSpawnerConfig, IChunkSpawnerRunner, IEnemySpawnPermission, IActiveChunkQuery
    {
        [Header("Refs")]
        [SerializeField] Transform player;
        [SerializeField] GameObject worldMover;
        [SerializeField] CoinRailFiller coinRailFiller;
        [SerializeField] ObstacleRailFiller obstacleRailFiller;
        [SerializeField] RelicRailFiller relicRailFiller;
        [SerializeField] CoinFactory coinFactory;
        [SerializeField] ObstacleFactory obstacleFactory;
        [SerializeField] RelicFactory relicFactory;
        [SerializeField] MonoBehaviour biomeDirectorProvider; 
        [SerializeField] ChunkSpawnConfig tutorialConfig;
        [SerializeField] ChunkSpawnConfig normalConfig;

        [Header("Config")]
        [SerializeField] private ChunkSpawnConfig activeConfig;
        readonly List<ChunkRoot> active = new();
        ChunkRoot lastChunk => active.Count > 0 ? active[active.Count - 1] : null;
        float chunkLength;
        int nextSpawnZ;

        IChunkFiller coinFiller;
        IChunkFiller obstacleFiller;
        IChunkFiller relicFiller;
        IBiomeDirector biomeDirector;

        bool isSpawning;

        public event Action OnSpawnedChunk;

        public bool IsSpawning => isSpawning;
        public bool CanSpawnEnemies => isSpawning && (active.Count > 0 && active[0].CanSpawnEnemies && active[1].CanSpawnEnemies);


        void Awake()
        {
            coinFiller = coinRailFiller;
            obstacleFiller = obstacleRailFiller;
            relicFiller = relicRailFiller;
            biomeDirector = biomeDirectorProvider as IBiomeDirector;
            if (biomeDirector == null)
                Debug.LogError($"{nameof(ChunkSpawner)}: biomeDirectorProvider must implement IBiomeDirector.");
        }

        public void SetConfig(ChunkSpawnConfig config)
        {
            if (config == null) return;
            activeConfig = config;
        }

        public void UseTutorialConfig()
        {
            SetConfig(tutorialConfig);
        }

        public void UseNormalConfig()
        {
            SetConfig(normalConfig);
        }

        public void StartSpawning()
        {
            if (isSpawning) return;
            if (biomeDirector == null) return;

            biomeDirector.ResetRun();

            isSpawning = true;
            nextSpawnZ = (int)(player.position.z + activeConfig.startAheadDistance);

            FillOneAhead(false);
            nextSpawnZ += Mathf.RoundToInt(chunkLength);
            FillAhead();
            FindAnyObjectByType<CoinRailGenerator>()?.ResetPathAt(player.position.z);
        }

        public void StopSpawning()
        {
            if (!isSpawning) return;

            isSpawning = false;
        }

        

        void FixedUpdate()
        {
            if (!isSpawning) return;

            if (active.Count > 0)
            {
                var first = active[0];
                if(lastChunk != null && nextSpawnZ - chunkLength >= lastChunk.transform.position.z)
                {
                    FillOneAhead();
                    OnSpawnedChunk?.Invoke();
                }
                if (player.position.z - first.transform.position.z > activeConfig.despawnBehindDistance)
                {
                    active.RemoveAt(0);
                    first.OwnerFactory?.Recycle(first);  // recycle to the same factory
                }                
            }
        }

        void FillAhead()
        {
            while (active.Count < activeConfig.keepAhead)
            {
                FillOneAhead();
                nextSpawnZ += Mathf.RoundToInt(chunkLength);
            }
            // Adjust nextSpawnZ to account for despawn distance
            nextSpawnZ = (Mathf.RoundToInt(chunkLength) * activeConfig.keepAhead) - Mathf.RoundToInt(activeConfig.despawnBehindDistance);
        }

        ChunkRoot FillOneAhead(bool spawnObstacles = true)
        {
            if (biomeDirector == null) return null;


            // Ask the director what to spawn next
            bool allowObstacles;

            var factory = biomeDirector.GetNextFactory(out allowObstacles);
            if (factory == null) return null;

            // If caller wants no obstacles (e.g. first chunk), that wins.
            bool finalSpawnObstacles = spawnObstacles && allowObstacles;


            //var factory = GetRandomFactory();
            float zPosition = lastChunk != null ? lastChunk.transform.position.z + lastChunk.ChunkLength : nextSpawnZ;
            ChunkRoot chunk = factory.Spawn(new Vector3(0f, 0f, zPosition), Quaternion.identity, worldMover.transform);
            chunk.Biome = biomeDirector.CurrentBiome;
            chunk.InjectFactories(coinFactory, obstacleFactory, relicFactory);
            coinFiller.FillChunk(chunk);
            if (finalSpawnObstacles && chunk.CanSpawnObstacles) obstacleFiller.FillChunk(chunk);
            relicFiller?.FillChunk(chunk);
            // Mark the owner factory so we can recycle correctly
            chunk.OwnerFactory = factory;
            chunkLength = chunk.ChunkLength;
            active.Add(chunk);

            return chunk;
        }

        public ChunkRoot GetChunkContaining(float worldZ)
        {
            for (int i = 1; i < active.Count; i++)
            {
                var c = active[i];
                float start = c.transform.position.z;
                if (worldZ < start) break;                 // sorted ascending → nothing further can contain it
                if (worldZ < start + c.ChunkLength) return c;
            }
            return null;
        }

    }
}
