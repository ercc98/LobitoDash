using System;
using System.Collections;
using ErccDev.Foundation.Audio;
using ErccDev.Foundation.Camera;
using ErccDev.Foundation.Core.Gameplay;
using ErccDev.Foundation.Core.Tutorial;
using ErccDev.Foundation.Data;
using ErccDev.Foundation.Input.Swipe;
using MagicVillageDash.Audio;
using MagicVillageDash.Camera;
using MagicVillageDash.Character;
using MagicVillageDash.Data;
using MagicVillageDash.Enemies;
using MagicVillageDash.Enemy;
using MagicVillageDash.FireBaseScripts;
using MagicVillageDash.Input;
using MagicVillageDash.Score;
using MagicVillageDash.Tutorial;
using MagicVillageDash.World;
using UnityEngine;

namespace MagicVillageDash.Runner
{
    public sealed class RunController : GameSessionController
    {
        public enum LaneBehavior { Yield, Block, Swap }
        [Header("Dependencies")]
        [SerializeField] private CameraShaker cameraShakerProvider;
        [SerializeField] private ParticleSystem hitCharactersParticlesProvider;
        [SerializeField] private MonoBehaviour runScoreSystemProvider;
        [SerializeField] private MonoBehaviour distanceTrackerProvider;
        [SerializeField] private MonoBehaviour coinCounterProvider;
        [SerializeField] private MonoBehaviour gameSpeedProvider;
        [SerializeField] private MonoBehaviour playerLaneMoverProvider;
        [SerializeField] private MonoBehaviour enemySpawnerProvider;
        [SerializeField] private MonoBehaviour turorialManagerProvider;
        [SerializeField] private MonoBehaviour tutorialContextBuilderProvider;
        [SerializeField] private MonoBehaviour chunkSpawnerProvider;
        [SerializeField] private MonoBehaviour enemyspawnPermissionProvider;
        [SerializeField] private MonoBehaviour swipeInputProvider;      // ISwipeInput
        [SerializeField] private MonoBehaviour runnerInputControllerProvider;

        [Header("Config")]
        public PlayerProfileData playerProfileData;

        ICameraShaker               cameraShaker;
        IEnemySpawner               enemySpawner;
        IRunScoreSystem             runScoreSystem;
        IDistanceTracker            distanceTracker;
        ICoinCounter                coinCounter;
        IGameSpeedController        gameSpeedController;
        ILaneMover                  enemyLaneMover;
        ILaneMover                  playerLaneMover;
        IMovementController         playerMovementController;
        IMovementController         enemyMovementController;
        ITutorialManager            tutorialManager;
        ITutorialManagerConfig      tutorialManagerConfig;
        ITutorialContextBuilder     tutorialContextBuilder;
        IChunkSpawnerConfig         chunkSpawnerConfig;
        IChunkSpawnerRunner         chunkSpawnerRunner;
        IEnemySpawnPermission       enemySpawnPermission;
        IRunnerInputController      runnerInputController;
        ISwipeInput                 swipeInput;

        
        [Header("Behavior")]
        [SerializeField] private LaneBehavior behavior = LaneBehavior.Swap;
        [SerializeField] private float reactDelay = 0.06f;
        [SerializeField] private bool oneReactionAtATime = true;
        private bool haveEnemySpawned = false;
        private Coroutine reactionRoutine;
        private Coroutine enemySpawnRoutine;

        void Awake()
        {
            runnerInputController = runnerInputControllerProvider as IRunnerInputController ?? FindAnyObjectByType<RunnerSwipeController>(FindObjectsInactive.Exclude);
            cameraShaker = cameraShakerProvider as ICameraShaker ?? cameraShakerProvider.GetComponent<ICameraShaker>();
            enemySpawner = enemySpawnerProvider as IEnemySpawner ?? FindAnyObjectByType<EnemySpawnManager>(FindObjectsInactive.Exclude);
            runScoreSystem = runScoreSystemProvider as IRunScoreSystem ?? FindAnyObjectByType<RunScoreSystem>(FindObjectsInactive.Exclude);
            distanceTracker = distanceTrackerProvider as IDistanceTracker ?? FindAnyObjectByType<DistanceTracker>(FindObjectsInactive.Exclude);
            coinCounter = coinCounterProvider as ICoinCounter ?? FindAnyObjectByType<CoinCounter>(FindObjectsInactive.Exclude);
            gameSpeedController = gameSpeedProvider as IGameSpeedController ?? FindAnyObjectByType<GameSpeedController>(FindObjectsInactive.Exclude);
            playerLaneMover = playerLaneMoverProvider as ILaneMover ?? FindAnyObjectByType<LaneRunner>(FindObjectsInactive.Exclude);
            playerMovementController = playerLaneMoverProvider as IMovementController ?? playerLaneMoverProvider.GetComponent<IMovementController>();
            tutorialManager = turorialManagerProvider as ITutorialManager ?? FindAnyObjectByType<TutorialManager>(FindObjectsInactive.Exclude);
            tutorialManagerConfig = turorialManagerProvider as ITutorialManagerConfig ?? turorialManagerProvider.GetComponent<ITutorialManagerConfig>();
            tutorialContextBuilder = tutorialContextBuilderProvider as ITutorialContextBuilder ?? tutorialContextBuilderProvider.GetComponent<ITutorialContextBuilder>();            
            chunkSpawnerConfig = chunkSpawnerProvider as IChunkSpawnerConfig ?? chunkSpawnerProvider.GetComponent<IChunkSpawnerConfig>();
            chunkSpawnerRunner = chunkSpawnerProvider as IChunkSpawnerRunner ?? chunkSpawnerProvider.GetComponent<IChunkSpawnerRunner>();
            enemySpawnPermission = enemyspawnPermissionProvider as IEnemySpawnPermission ?? enemyspawnPermissionProvider.GetComponent<IEnemySpawnPermission>();
            swipeInput = swipeInputProvider as ISwipeInput ?? swipeInputProvider.GetComponent<ISwipeInput>();
        }

        void OnEnable()
        {
            GameEvents.GameOver += OnGameOver;
            GameEvents.GameStarted += OnGameStarted;


            if (playerLaneMover != null) playerLaneMover.OnLaneChangeAttempt += OnPlayerAttempt;
            if (enemySpawner != null) enemySpawner.OnSpawnedEnemy += OnEnemySpawned;
            if (enemySpawner != null) enemySpawner.OnStartSpawnEnemy += OnEnemySpawnedAttempt;
            if (tutorialManager != null) tutorialManager.OnTutorialEnded += OnTutorialEnded;
            if (chunkSpawnerConfig != null)
            {
                if (playerProfileData.TutorialCompleted)
                    chunkSpawnerConfig.UseNormalConfig();
                else
                    chunkSpawnerConfig.UseTutorialConfig();
            }
            tutorialManagerConfig?.SetContext(tutorialContextBuilder.Build());
            runnerInputController.Deactivate();
        }

        void OnDisable()
        {
            if (chunkSpawnerRunner != null) chunkSpawnerRunner.OnSpawnedChunk -= OnChunkSpawned;
            if (playerLaneMover != null) playerLaneMover.OnLaneChangeAttempt -= OnPlayerAttempt;
            if (enemySpawner != null) enemySpawner.OnSpawnedEnemy -= OnEnemySpawned;
            if (enemySpawner != null) enemySpawner.OnStartSpawnEnemy -= OnEnemySpawnedAttempt;

            if (enemyLaneMover != null) enemyLaneMover.OnLaneChangeAttempt -= OnEnemyAttempt;

            if (reactionRoutine != null) { StopCoroutine(reactionRoutine); reactionRoutine = null; }
            if  (enemySpawnRoutine != null) { StopCoroutine(enemySpawnRoutine); enemySpawnRoutine = null; }

            if (tutorialManager != null ) tutorialManager.OnTutorialEnded -= OnTutorialEnded;
            GameEvents.GameOver -= OnGameOver;
            GameEvents.GameStarted -= OnGameStarted;
        }


        #region Events Methods

        private void OnGameStarted()
        {
            Debug.Log("RunController: OnGameStarted called");
            AudioManager.Instance?.Play(MusicId.GameTheme2);
            coinCounter?.ResetCoins(0);
            distanceTracker?.ResetDistance();
            gameSpeedController?.ResetSpeed();
            runScoreSystem?.ResetRun();
            distanceTracker?.StartRun();

            FirebaseAnalyticsService.Instance.LogStartPlaying();

            if (playerProfileData.TutorialCompleted)
            {
                enemySpawner?.Spawn();
                haveEnemySpawned = true;
                runnerInputController.Activate();
                if (chunkSpawnerRunner != null) chunkSpawnerRunner.OnSpawnedChunk += OnChunkSpawned;
            }
            else
            {
                tutorialManager?.StartTutorial();
            }

            chunkSpawnerRunner?.StartSpawning();
        }
        private void OnChunkSpawned()
        {   
            if (!enemySpawnPermission.CanSpawnEnemies || haveEnemySpawned) return;
            enemySpawner?.Spawn();
            haveEnemySpawned = true;
        }

        private void OnTutorialEnded()
        {
            enemySpawner?.Spawn();
            haveEnemySpawned = true;
            runnerInputController.Activate();
            playerProfileData.TutorialCompleted = true;
            GameDataService._instance.SaveAll();
            if (chunkSpawnerRunner != null) chunkSpawnerRunner.OnSpawnedChunk += OnChunkSpawned;
        }

        private void OnGameOver()
        {
            AudioManager.Instance?.StopLoop(SoundCategory.Music);
            distanceTracker?.StopRun();
            runScoreSystem?.CommitIfBest();
            gameSpeedController?.SetSpeed(0f);
            FirebaseAnalyticsService.Instance.LogPlayerDied(distanceTracker.CurrentDistance, coinCounter.Coins);
        }

        private void OnEnemyStartSpawned(int value)
        {
            OnEnemyAttempt(value, value);
        }
        
        private void OnEnemySpawned(EnemyController enemy)
        {
            enemy.Ondied += OnEnemyDespawned;
            enemyLaneMover = enemy.SelfLaneMover;
            enemyMovementController = enemy;
            enemyLaneMover.OnLaneChangeAttempt += OnEnemyAttempt;
        }

        private void OnEnemyDespawned(EnemyController enemy)
        {
            haveEnemySpawned = false;
            enemyMovementController = null;
            enemy.Ondied -= OnEnemyDespawned;
            enemyLaneMover.OnLaneChangeAttempt -= OnEnemyAttempt;
            enemyLaneMover = null;
            //SpawnEnemy();
            
        }        

        protected override void OnSessionStarted()
        {
        }

        protected override void OnSessionEnded()
        {
            runScoreSystem?.CommitIfBest();
            gameSpeedController?.SetSpeed(0f);
        }

        protected override void ResetSessionState()
        {
            runScoreSystem?.ResetRun();
            distanceTracker?.ResetDistance();
            coinCounter?.ResetCoins(0);
            gameSpeedController?.ResetSpeed();
        }
        #endregion

        #region Reaction Methods
        private void OnEnemyAttempt(int from, int to) => HandleAttempt(enemyLaneMover, playerLaneMover, playerMovementController, from, to);
        private void OnPlayerAttempt(int from, int to) => HandleAttempt(playerLaneMover, enemyLaneMover, enemyMovementController, from, to);
        private void OnEnemySpawnedAttempt(int to)
        {
            if (to != playerLaneMover.CurrentLane) return;
            if (oneReactionAtATime && reactionRoutine != null) return;
            DoYield(enemyLaneMover, playerLaneMover, playerMovementController, to, to);
        }

        private void HandleAttempt(ILaneMover mover, ILaneMover other, IMovementController otherController, int from, int to)
        {
            if(other == null) return;
            if (to != other.CurrentLane) return;

            if (oneReactionAtATime && reactionRoutine != null) return;

            reactionRoutine = StartCoroutine(ReactRoutine(mover, other, otherController, from, to));
        }

        private IEnumerator ReactRoutine(ILaneMover mover, ILaneMover other, IMovementController otherController, int from, int to)
        {
            if (reactDelay > 0f) yield return new WaitForSeconds(reactDelay);

            switch (behavior)
            {
                case LaneBehavior.Block: DoBlock(mover, other, otherController, from, to); break;
                case LaneBehavior.Yield: DoYield(mover, other, otherController, from, to); break;
                case LaneBehavior.Swap:  DoSwap(mover, other, otherController, from, to);  break;
            }

            reactionRoutine = null;
        }

        private void DoYield(ILaneMover mover, ILaneMover other, IMovementController otherController, int from, int to)
        {
            if (to < 2)
                otherController.TurnRight();
            else
                otherController.TurnLeft(); 
                
            /*
            if (otherController == null || other == null) return;

            int target = other.CurrentLane;

            // If you're on right half, yield left; otherwise yield right (simple heuristic)
            if (target >= 1) otherController.TurnLeft();
            else otherController.TurnRight();
            */
        }


        private void DoBlock(ILaneMover mover, ILaneMover other, IMovementController otherController, int from, int to)
        {
            if (from > other.CurrentLane) otherController.TurnLeft();
            else if (from < other.CurrentLane) otherController.TurnRight();  
            mover.SnapToLane(from);
            other.SnapToLane(other.CurrentLane);
            cameraShaker?.Shake(1.0f, 1.0f, 0.25f);
            SpawnHitVfx(mover, other);
        }

        private void DoSwap(ILaneMover mover, ILaneMover other, IMovementController otherController, int from, int to)
        {
            if (to > mover.CurrentLane) otherController.TurnLeft();
            else if (to < mover.CurrentLane) otherController.TurnRight();
        }
        
        #endregion


        private void SpawnHitVfx(ILaneMover mover, ILaneMover other)
        {
            if (!hitCharactersParticlesProvider) return;
            float lanePosx = (mover.CurrentLane + other.CurrentLane) * 0.5f * mover.LaneWidth - mover.LaneWidth;
            Vector3 position = new(lanePosx, mover is MonoBehaviour mb ? mb.transform.position.y : 0f, 0f);
            hitCharactersParticlesProvider.transform.SetPositionAndRotation(position, Quaternion.identity);
            hitCharactersParticlesProvider.Play();
            AudioManager.Instance?.Play("ElectricSwipe2", SoundCategory.SFX);
        }
    }
}
