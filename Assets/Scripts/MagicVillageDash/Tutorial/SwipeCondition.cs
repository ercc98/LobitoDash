using ErccDev.Foundation.Core.Tutorial;
using ErccDev.Foundation.Input;
using ErccDev.Foundation.Input.Swipe;
using MagicVillageDash.Character;
using UnityEngine;

namespace MagicVillageDash.Tutorial
{
    [CreateAssetMenu(menuName = "MagicVillageDash/Tutorial/Conditions/Swipe")]
    public class SwipeCondition : TutorialCondition
    {
        public enum SwipeDirection { Left, Right, Up, Down, Tap, StartTap, EndTap }
        public SwipeDirection requiredSwipe;
        //[SerializeField] private SwipeInputSystem swipeInputProvider;
        private ISwipeInput swipeInput;
        private ITouchInput touchInput;
        private IMovementController movementController;
        private bool completed;
        
        public override void Initialize(ITutorialContext context)
        {
            completed = false;

            swipeInput = context?.Get<ISwipeInput>();
            touchInput = context?.Get<ITouchInput>();
            movementController = context?.Get<IMovementController>();

#if UNITY_EDITOR
            if (swipeInput == null)
                Debug.LogWarning($"[{name}] SwipeCondition missing ISwipeInput in TutorialContext");
#endif

            if (swipeInput == null) return;

            switch (requiredSwipe)
            {
                case SwipeDirection.Left: swipeInput.SwipeLeft += OnSwipeLeft; break;
                case SwipeDirection.Right: swipeInput.SwipeRight += OnSwipeRight; break;
                case SwipeDirection.Up: swipeInput.SwipeUp += OnSwipeUp; break;
                case SwipeDirection.Down: swipeInput.SwipeDown += OnSwipeDown; break;
                //case SwipeDirection.Tap: touchInput.Tap += OnTap; break;
                case SwipeDirection.StartTap: touchInput.StartTouch += OnTouchStart; break;
                case SwipeDirection.EndTap: touchInput.EndTouch += OnTouchEnd; break;
            }
        }

        private void OnSwipeLeft()
        {
            OnSwipe(requiredSwipe);
            movementController.TurnLeft();
        }

        private void OnSwipeRight()
        {
            OnSwipe(requiredSwipe);
            movementController.TurnRight();
        }

        private void OnSwipeUp()
        {
            OnSwipe(requiredSwipe);
            movementController.Jump();
        }

        private void OnSwipeDown()
        {
            OnSwipe(requiredSwipe);
            movementController.Crouch(true);
        }
        private void OnTap()
        {
            OnSwipe(requiredSwipe);
            movementController.Jump();
        }

        public void OnTouchStart()
        {
            OnSwipe(requiredSwipe);
            movementController.Defend(true);
        }
        public void OnTouchEnd()
        {
            OnSwipe(requiredSwipe);
            movementController.Defend(false);
        }


        private void OnSwipe(SwipeDirection dir)
        {
            if (dir == requiredSwipe)
                completed = true;
        }

        public override bool IsCompleted() => completed;

        public override void Cleanup()
        {
            if (swipeInput == null) return;

            swipeInput.SwipeLeft -= OnSwipeLeft;
            swipeInput.SwipeRight -= OnSwipeRight;
            swipeInput.SwipeUp -= OnSwipeUp;
            swipeInput.SwipeDown -= OnSwipeDown;
            swipeInput.Tap -= OnTap;
            touchInput.StartTouch -= OnTouchStart;
            touchInput.EndTouch -= OnTouchEnd;

            swipeInput = null;
            movementController = null;
        }
    }
}