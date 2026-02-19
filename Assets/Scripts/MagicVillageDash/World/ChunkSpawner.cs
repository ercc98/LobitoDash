using UnityEngine;
using System.Collections.Generic;
using MagicVillageDash.Collectibles;
using MagicVillageDash.Obstacles;
using MagicVillageDash.World.Biomes;
using System;


namespace MagicVillageDash.World
{
    public sealed class ChunkSpawner : MonoBehaviour, IChunkSpawnerConfig, IChunkSpawnerRunner, IEnemySpawnPermission
    {
        [Header("Refs")]
        [SerializeField] Transform player;
        [SerializeField] GameObject worldMover;
        [SerializeField] CoinRailFiller coinRailFiller;
        [SerializeField] ObstacleRailFiller obstacleRailFiller;
        [SerializeField] CoinFactory coinFactory;
        [SerializeField] ObstacleFactory obstacleFactory;
        [SerializeField] MonoBehaviour biomeDirectorProvider; 
        [SerializeField] ChunkSpawnConfig tutorialConfig;
        [SerializeField] ChunkSpawnConfig normalConfig;

        [Header("Config")]
        [SerializeField] private ChunkSpawnConfig activeConfig;
        readonly List<ChunkRoot> active = new();
        float chunkLength;
        int nextSpawnZ;

        IChunkFiller coinFiller;
        IChunkFiller obstacleFiller;
        IBiomeDirector biomeDirector;

        bool isSpawning;

        public event Action OnSpawnedChunk;

        public bool IsSpawning => isSpawning;
        public bool CanSpawnEnemies => isSpawning && (active.Count > 0 && active[0].CanSpawnEnemies && active[1].CanSpawnEnemies);


        void Awake()
        {
            coinFiller = coinRailFiller;
            obstacleFiller = obstacleRailFiller;
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
                if (player.position.z - first.transform.position.z > activeConfig.despawnBehindDistance)
                {
                    active.RemoveAt(0);
                    first.OwnerFactory?.Recycle(first);  // recycle to the same factory
                    FillOneAhead();
                    OnSpawnedChunk?.Invoke();
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

            ChunkRoot chunk = factory.Spawn(new Vector3(0f, 0f, nextSpawnZ), Quaternion.identity, worldMover.transform);
            chunk.InjectFactories(coinFactory, obstacleFactory);
            coinFiller.FillChunk(chunk);
            if (finalSpawnObstacles && chunk.CanSpawnObstacles) obstacleFiller.FillChunk(chunk);
            // Mark the owner factory so we can recycle correctly
            chunk.OwnerFactory = factory;
            chunkLength = chunk.ChunkLength;
            active.Add(chunk);

            return chunk;
        }
        
        /*
        ChunkFactory GetRandomFactory()
        {
            if (factories == null || factories.Length == 0) return null;

            // Pick a random non-null factory
            for (int tries = 0; tries < 8; tries++)
            {
                var f = factories[Random.Range(0, factories.Length)];
                if (f != null) return f;
            }

            // Fallback: first non-null
            foreach (var f in factories)
                if (f != null) return f;

            return null;            
        }
        */

    }
}
