using System;
using System.Collections;
using ErccDev.Foundation.Audio;
using ErccDev.Foundation.Core.Gameplay;
using MagicVillageDash.Audio;
using MagicVillageDash.Character;
using MagicVillageDash.Character.CharacterAnimator;
using MagicVillageDash.Runner;
using UnityEngine;

namespace MagicVillageDash.Enemy
{
    [DisallowMultipleComponent]
    public class EnemyController : CharacterControllerBase
    {
        [Header("References")]
        [SerializeField] private MonoBehaviour playerLaneMoverProvider;
        [SerializeField] private CharacterController playerCharacterController;
        [SerializeField] private int coinsOnDeath = 1;
        [SerializeField] private Vector3 rewardOffset = Vector3.up;

        

        [Header("Safety")]
        [Tooltip("Failsafe: if something goes wrong, collisions auto-reenable after this many seconds.")]
        [SerializeField] private float collisionsFailSafe = 1.0f;

        private CharacterController selfCharacterController;
        private Coroutine collisionsTimeoutCo;
        private Coroutine collisionsSelfCCTimeoutCo;

        public ILaneMover player;
        public ILaneMover SelfLaneMover => selfLaneMover;
        public IExpressionAnimator selfExpressionAnimator;

        /// <summary>Raised when the enemy is death. Factory listens and recycles.</summary>
        public event Action<EnemyController> Ondied;
        public event Action<int, Vector3> OnReward; 

        protected override void Awake()
        {
            base.Awake();

            selfCharacterController = GetComponent<CharacterController>();
            player = playerLaneMoverProvider as ILaneMover ?? playerLaneMoverProvider?.GetComponent<ILaneMover>();
            selfExpressionAnimator = selfAnimatorControllerProvider;
            if (player == null) Debug.LogError($"{name}: Missing player ILaneMover provider.", this);
            if (playerCharacterController == null) Debug.LogError($"{name}: Missing player CharacterController reference.", this);

        }

        void OnEnable()
        {
            if (player != null) player.OnLaneChangeAttempt += OnPlayerAttempt;
            if (selfLaneMover != null) selfLaneMover.OnLaneChanged += OnSelfLaneChanged;
            GameEvents.GameOver += OnGameOver;
            BeginNonCollidingSelfCCWindow();
            AudioManager.Instance?.Play("Bark", SoundCategory.Voice);
        }

        private void BeginNonCollidingSelfCCWindow()
        {
            selfCharacterController.detectCollisions = false;
            if (collisionsSelfCCTimeoutCo != null) StopCoroutine(collisionsSelfCCTimeoutCo);
            collisionsSelfCCTimeoutCo = StartCoroutine(ReenableSelfCCAfterTimeout(collisionsFailSafe));
        }
        
        private IEnumerator ReenableSelfCCAfterTimeout(float t)
        {
            yield return new WaitForSeconds(t);
            selfCharacterController.detectCollisions = true;
            collisionsSelfCCTimeoutCo = null;
        }   

        void OnDisable()
        {
            if (player != null) player.OnLaneChangeAttempt -= OnPlayerAttempt;
            if (selfLaneMover != null) selfLaneMover.OnLaneChanged -= OnSelfLaneChanged;
            GameEvents.GameOver -= OnGameOver;
            SetCollisions(true);
        }
        private void OnPlayerAttempt(int from, int to)
        {
            if (to != selfLaneMover.CurrentLane) return;
            BeginNonCollidingWindow();
        }        

        private void OnSelfLaneChanged(int from, int to) => SetCollisions(true);

        private void BeginNonCollidingWindow()
        {
            SetCollisions(false);
            if (collisionsTimeoutCo != null) StopCoroutine(collisionsTimeoutCo);
            collisionsTimeoutCo = StartCoroutine(ReenableAfterTimeout(collisionsFailSafe));
        }
        
        private IEnumerator ReenableAfterTimeout(float t)
        {
            yield return new WaitForSeconds(t);
            SetCollisions(true);
            collisionsTimeoutCo = null;
        }     

        private void SetCollisions(bool enabled)
        {
            if (!selfCharacterController || !playerCharacterController) return;
            Physics.IgnoreCollision(selfCharacterController, playerCharacterController, !enabled);
        }

        internal void SetSpawnPose(int laneIndex)
        {
            selfLaneMover.SnapToLane(laneIndex);
        }

        protected override void OnHazardHitInternal(Vector3 hazardHitPosition)
        {
            OnReward?.Invoke(coinsOnDeath, transform.position + rewardOffset);
            StartCoroutine(WaitForDie(0.5f));
        }

        private IEnumerator WaitForDie(float t)
        {
            yield return new WaitForSeconds(t);
            Ondied?.Invoke(this);
            gameObject.SetActive(false);
        }

        private void OnGameOver()
        {
            selfExpressionAnimator.Excited(true);
        }
    }
}
