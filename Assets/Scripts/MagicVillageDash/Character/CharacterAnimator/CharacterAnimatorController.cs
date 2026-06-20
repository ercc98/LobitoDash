using UnityEngine;

namespace MagicVillageDash.Character.CharacterAnimator
{
    public class CharacterAnimatorController : MonoBehaviour, IMovementAnimator, IDeathAnimator, IExpressionAnimator
    {
        [SerializeField] private Animator animatorController;
        public Animator AnimatorController { private get => animatorController; set => animatorController = value; }
        [SerializeField] protected ParticleSystem landingDustParticles;
        public void LandingDust()
        {
            landingDustParticles.transform.position = transform.position + Vector3.up * 0.1f; 
            landingDustParticles?.Play();

        }  
        bool IsJumping()
        {
            return animatorController.GetCurrentAnimatorStateInfo(0).IsName("Jump");
        }
        public void Crouch(bool isCrouching)
        {
            //animatorController.SetBool("IsCrouching", isCrouching);
        }

        public void Defend(bool isDefending)
        {
            animatorController.SetBool("IsDefending", isDefending);
        }

        public void Walk(bool isWalking)
        {
            animatorController.SetBool("Walk", isWalking);
        }
        
        public void Jump()
        {
            animatorController.SetTrigger("Jump");
        }

        public void Landing()
        {
            animatorController.SetTrigger("Land");
        }

        public void MovingSpeed(float speed)
        {
            if (speed > 0) animatorController.SetBool("Walk", true);
            else animatorController.SetBool("Walk", false);
            
            animatorController.SetFloat("Speed", speed);
        }

        public void TurnLeft()
        {
            if (IsJumping()) return;
            animatorController.SetTrigger("RightSpin");
        }

        public void TurnRight()
        {
            if (IsJumping()) return;
            animatorController.SetTrigger("LeftSpin");
        }

        public void Die()
        {
            animatorController.SetBool("Dead", true);
        }

        public void Revive()
        {
            animatorController.SetBool("Dead", false);
        }

        public void SetDead(bool isDead)
        {
            animatorController.SetBool("Dead", isDead);
        }

        public void Idle()
        {
            animatorController.SetBool("Walk", false);
        }

        public void Excited(bool value)
        {
            animatorController.SetBool("Excited", value);
        }

        // Idle expressions (triggers)
        public void Dig()
        {
            animatorController.SetTrigger("Dig");
        }

        public void Scratch()
        {
            animatorController.SetTrigger("Scratch");
        }

        public void Smell()
        {
            animatorController.SetTrigger("Smell");
        }

        public void Yawn()
        {
            animatorController.SetTrigger("Yawn");
        }

        // Tap expressions
        public void Bark()
        {
            animatorController.SetTrigger("Bark");
        }

        public void Howl()
        {
            animatorController.SetTrigger("Howl");
        }

        public void Lie(bool value)
        {
            animatorController.SetBool("Lie", value);
        }

        public void Sleep(bool value)
        {
            animatorController.SetBool("Sleep", value);
        }
    }
}
