using System.Collections;
using UnityEngine;
using MagicVillageDash.Enemy;
using ErccDev.Foundation.Core.Gameplay;
using System;
using MagicVillageDash.Audio;
using ErccDev.Foundation.Audio;
using Random = UnityEngine.Random;

namespace MagicVillageDash.Enemies
{
    public sealed class EnemySpawnManager : MonoBehaviour, IEnemySpawner
    {
        [SerializeField] private static WaitForSeconds _waitForSeconds1 = new(1.5f);
        [Header("Factory & Parent")]
        [SerializeField] private MonoBehaviour enemyFactoryProvider;

        [Header("Lanes")]
        [SerializeField] private int numberOfLanes = 3;
        [SerializeField] private float laneWidth = 2.2f;

        [Header("Respawn")]
        [SerializeField] private Vector3 spawnPosition = new Vector3(0, 1, 2);
        [SerializeField] private float respawnDelay = 1.5f;
        [SerializeField] private ParticleSystem spawnAreaParticleSystem;


        IEnemyFactory enemyFactory;
        private Coroutine respawnRoutine;
        public event Action<EnemyController> OnSpawnedEnemy;
        public event Action<int> OnStartSpawnEnemy;

        void Awake()
        {
            enemyFactory = enemyFactoryProvider as IEnemyFactory ?? FindAnyObjectByType<EnemyFactory>(FindObjectsInactive.Exclude);
        }

        void OnEnable()
        {
            GameEvents.GameOver += OnGameOver;
        }

        void OnDisable()
        {
            GameEvents.GameOver -= OnGameOver;
            if (respawnRoutine != null)
            {
                StopCoroutine(respawnRoutine);
                respawnRoutine = null;
            }
        }
        
        public void Spawn()
        {
            respawnRoutine = StartCoroutine(SpawnEnemyAfterDelay(respawnDelay, Random.Range(0, numberOfLanes)));
        }
        IEnumerator SpawnEnemyAfterDelay(float respawnDelay, int lane)
        {
            yield return new WaitForSeconds(respawnDelay);
            spawnPosition.x = laneWidth * (lane - 1);
            spawnAreaParticleSystem.transform.position = spawnPosition - new Vector3(0, 0.95f, 0);
            spawnAreaParticleSystem.Play();
            AudioManager.Instance?.Play("SpawnAreaEnemy", SoundCategory.SFX);
            yield return _waitForSeconds1;
            OnStartSpawnEnemy?.Invoke(lane);
            OnSpawnedEnemy?.Invoke(Spawn(lane));
        }

        public EnemyController Spawn(int lane)
        {
            EnemyController spawnedEnemy = enemyFactory.Spawn(spawnPosition, Quaternion.identity);
            spawnedEnemy.SelfLaneMover.SnapToLane(lane);
            AudioManager.Instance?.Play("SpawnEnemy", SoundCategory.SFX);
            return spawnedEnemy;
        }

        private void OnGameOver()
        {
            if (respawnRoutine != null)
            {
                StopCoroutine(respawnRoutine);
                respawnRoutine = null;
            }
        }

    }
}
