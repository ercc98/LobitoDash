using UnityEngine;
using ErccDev.Foundation.Input.Swipe;
using MagicVillageDash.Character;
using MagicVillageDash.Player;
using ErccDev.Foundation.Core.Gameplay;
using System;
using MagicVillageDash.Runner;
using ErccDev.Foundation.Input;

namespace MagicVillageDash.Input
{
    [DisallowMultipleComponent]
    public sealed class RunnerSwipeController : MonoBehaviour, IRunnerInputController 
    {
        [Header("Providers")]
        [SerializeField] private MonoBehaviour playerControllerProvider;
        [SerializeField] private MonoBehaviour inputProvider;

        [Header("Options")]
        private IMovementController movementController;
        private ISwipeInput swipeInput;
        private ITouchInput touchInput;
        private bool active;
        public bool IsActive => active;

        private void Awake()
        {
            swipeInput = inputProvider as ISwipeInput ?? FindAnyObjectByType<SwipeInputSystem>(FindObjectsInactive.Exclude);
            touchInput = inputProvider as ITouchInput ?? FindAnyObjectByType<SwipeInputSystem>(FindObjectsInactive.Exclude);
            movementController = playerControllerProvider as IMovementController ?? FindAnyObjectByType<PlayerController>(FindObjectsInactive.Exclude);
        }

        private void OnEnable()
        {
            GameEvents.GameOver += OnGameOver;
            Activate();            
        }

        private void OnDisable()
        {
            GameEvents.GameOver -= OnGameOver;
            Deactivate();
        }

        private void OnGameOver()
        {
            Deactivate();
        }

        private void OnSwipeLeft()  => TurnLeft();
        private void OnSwipeRight() => TurnRight();
        private void OnSwipeUp()    => Jump();
        private void OnSwipeDown()  => Crouch(true);
        private void OnTap() => Jump();
        private void OnStartTouch() => Defend(true);
        private void OnEndTouch() => Defend(false);

        public void Activate()
        {
            if (active) return;
            swipeInput.SwipeLeft+= OnSwipeLeft;
            swipeInput.SwipeRight += OnSwipeRight;
            swipeInput.SwipeUp += OnSwipeUp;
            swipeInput.SwipeDown += OnSwipeDown;
            //swipeInput.Tap += OnTap;
            touchInput.StartTouch += OnStartTouch;
            touchInput.EndTouch += OnEndTouch;
            active = true;
        }

        public void Deactivate()
        {
            if (!active) return;
            swipeInput.SwipeLeft -= OnSwipeLeft;
            swipeInput.SwipeRight -= OnSwipeRight;
            swipeInput.SwipeUp -= OnSwipeUp;
            swipeInput.SwipeDown -= OnSwipeDown;
            //swipeInput.Tap -= OnTap;
            touchInput.StartTouch -= OnStartTouch;
            touchInput.EndTouch -= OnEndTouch;
            active = false;
        }

        private void TurnLeft()
        {
            movementController.TurnLeft();
        }

        private void TurnRight()
        {
            movementController.TurnRight();
        }

        private void Jump()
        {
            movementController.Jump();
        }
        private void Defend(bool isDefending)
        {
            movementController.Defend(isDefending);
        }

        private void Crouch(bool isCrouching)
        {
            movementController.Crouch(isCrouching);
        }
    }
}
