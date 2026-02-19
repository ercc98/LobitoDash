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
            
            float positionX = laneWidth * (lane - 1);
            spawnAreaParticleSystem.transform.position = new Vector3(positionX, 0.05f, 0);
            spawnAreaParticleSystem.Play();
            AudioManager.Instance?.Play("SpawnAreaEnemy", SoundCategory.SFX);
            yield return _waitForSeconds1;
            OnStartSpawnEnemy?.Invoke(lane);
            OnSpawnedEnemy?.Invoke(Spawn(positionX, lane));
        }

        public EnemyController Spawn(float positionX, int lane)
        {
            EnemyController spawnedEnemy = enemyFactory.Spawn(new Vector3(positionX, 1, 0), Quaternion.identity);
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
