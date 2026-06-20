using ErccDev.Foundation.Audio;
using MagicVillageDash.Audio;
using MagicVillageDash.Character.CharacterAnimator;
using MagicVillageDash.Gameplay;
using MagicVillageDash.Runner;
using MagicVillageDash.World;
using UnityEngine;

namespace MagicVillageDash.Character
{
    public abstract class CharacterControllerBase : MonoBehaviour, IMovementController, IHazardReceiver
    {
        [Header("References")]
        [SerializeField] protected CharacterAnimatorController selfAnimatorControllerProvider; 
        [SerializeField] protected ParticleSystem hitHazardParticlesProvider;
        [SerializeField] protected ParticleSystem turnRightParticles;
        [SerializeField] protected ParticleSystem turnLeftParticles;     
        [SerializeField] protected ParticleSystem landingDustParticles;   
        [SerializeField] protected MonoBehaviour gameSpeedProvider;

        protected ILaneMover selfLaneMover;
        protected IMovementAnimator selfMovementAnimator;
        protected IDeathAnimator selfDeathAnimator;
        protected IGameSpeedController gameSpeedController;

        protected virtual void Awake()
        {
            selfLaneMover = GetComponent<ILaneMover>();
            selfMovementAnimator = selfAnimatorControllerProvider;
            selfDeathAnimator = selfAnimatorControllerProvider;
            gameSpeedController = gameSpeedProvider as IGameSpeedController
                ?? FindAnyObjectByType<GameSpeedController>(FindObjectsInactive.Exclude);

            if (selfLaneMover == null) Debug.LogError($"{name}: Missing ILaneMover.", this);
            if (selfMovementAnimator == null) Debug.LogError($"{name}: Missing animator provider.", this);
            if (gameSpeedController == null) Debug.LogError($"{name}: Missing gameSpeedProvider / GameSpeedController.", this);
        }

        protected virtual void OnEnable()
        {
            selfMovementAnimator.Walk(true);
            landingDustParticles.transform.position = transform.position + Vector3.up * -0.5f; 
            landingDustParticles?.Play();
        }

        public virtual void TurnLeft()
        {
            selfLaneMover.MoveLeft();
            selfMovementAnimator.TurnLeft();
            if (turnLeftParticles) turnLeftParticles.Play();
            if (turnRightParticles) turnRightParticles.Play();
            AudioManager.Instance?.Play("ElectricSwipe", SoundCategory.SFX);
        }

        public virtual void TurnRight()
        {
            selfLaneMover.MoveRight();
            selfMovementAnimator.TurnRight();
            if (turnRightParticles) turnRightParticles.Play();
            if (turnLeftParticles) turnLeftParticles.Play();
            AudioManager.Instance?.Play("ElectricSwipe", SoundCategory.SFX);
        }

        public virtual void MovingSpeed(float speed)
        {
            selfMovementAnimator.MovingSpeed(speed);
        }

        public virtual void Crouch(bool isCrouching)
        {
            //selfLaneMover.Slide();
            selfMovementAnimator.Crouch(isCrouching);
        }

        public virtual void Jump()
        {
            selfLaneMover.Jump();
            selfMovementAnimator.Jump();

        }
        
        

        public virtual void Defend(bool isDefending)
        {
            selfMovementAnimator.Defend(isDefending);
        }
        public virtual void Idle()
        {
            selfMovementAnimator.Idle();
        }

        public void OnHazardHit(Vector3 hazardHitPosition)
        {
            AudioManager.Instance?.Play("Bomb", SoundCategory.SFX);
            AudioManager.Instance?.Play("Die", SoundCategory.Voice);
            selfDeathAnimator.Die();
            SpawnHitVfx(hazardHitPosition);
            OnHazardHitInternal(hazardHitPosition);
        }

        protected abstract void OnHazardHitInternal(Vector3 hazardHitPosition);

        protected void SpawnHitVfx(Vector3 hitPosition)
        {
            if (!hitHazardParticlesProvider) return;
            hitHazardParticlesProvider.transform.SetPositionAndRotation(hitPosition, Quaternion.identity);
            hitHazardParticlesProvider.Play();
        }

        
    }
}
