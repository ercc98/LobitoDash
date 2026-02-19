using UnityEngine;
using ErccDev.Foundation.Core.Tutorial;
using ErccDev.Foundation.Input.Swipe;
using MagicVillageDash.Character;
using MagicVillageDash.Player;
using ErccDev.Foundation.Input;

namespace MagicVillageDash.Tutorial
{
    public sealed class TutorialContextBuilder : MonoBehaviour, ITutorialContextBuilder
    {
        [Header("Providers (assign in Inspector)")]
        [SerializeField] private MonoBehaviour swipeInputProvider;          
        [SerializeField] private MonoBehaviour movementControllerProvider;  

        [Header("Optional Auto-Find (fallback)")]
        [SerializeField] private bool autoFindIfMissing = true;

        public ITutorialContext Build()
        {
            var swipe = swipeInputProvider as ISwipeInput;
            var touch = swipeInputProvider as ITouchInput;
            var move  = movementControllerProvider as IMovementController;

            if (autoFindIfMissing)
            {
                if( touch == null)
                {
                    var touchMb = FindAnyObjectByType<SwipeInputSystem>(FindObjectsInactive.Exclude);
                    touch = touchMb as ITouchInput;
                }
                if (swipe == null)
                {
                    var swipeMb = FindAnyObjectByType<SwipeInputSystem>(FindObjectsInactive.Exclude);
                    swipe = swipeMb as ISwipeInput;
                }

                if (move == null)
                {
                    var moveMb = FindAnyObjectByType<PlayerController>(FindObjectsInactive.Exclude);
                    move = moveMb as IMovementController;
                }
            }

#if UNITY_EDITOR
            if( touch == null)
                Debug.LogWarning("[TutorialContextBuilder] Missing ITouchInput (assign swipeInputProvider).", this);
            if (swipe == null)
                Debug.LogWarning("[TutorialContextBuilder] Missing ISwipeInput (assign swipeInputProvider).", this);

            if (move == null)
                Debug.LogWarning("[TutorialContextBuilder] Missing IMovementController (assign movementControllerProvider).", this);
#endif

            return new TutorialContext()
                .Add<ITouchInput>(touch)
                .Add<ISwipeInput>(swipe)
                .Add<IMovementController>(move);
        }
    }
}