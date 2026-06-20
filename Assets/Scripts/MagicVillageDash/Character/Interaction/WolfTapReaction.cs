using UnityEngine;
using UnityEngine.InputSystem;
using MagicVillageDash.Character.CharacterAnimator;

namespace MagicVillageDash.Character.Interaction
{
    /// <summary>
    /// Tap/click the wolf and it plays a reaction expression.
    /// Reusable in any menu or scene (start, game-over, pause, den).
    ///
    /// Does its own raycast from <see cref="tapCamera"/> instead of relying on
    /// OnMouseDown, because in a URP camera stack only the overlay camera sees the
    /// wolf's layer (e.g. DenSlot) and OnMouseDown can't be trusted to use it.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class WolfTapReaction : MonoBehaviour
    {
        [Tooltip("Assign the CharacterAnimatorController (or any IExpressionAnimator).")]
        [SerializeField] private MonoBehaviour expressionSource;
        [Tooltip("The GameObject of the wolf to show/hide. Optional, but assign if you want the wolf to disappear when no panels are active.")]
        [SerializeField] private GameObject WolfGO;

        [Tooltip("Camera that actually renders the wolf's layer (the overlay camera in the den).")]
        [SerializeField] private Camera tapCamera;

        [Tooltip("Layers the tap ray is allowed to hit. Include the wolf's layer.")]
        [SerializeField] private LayerMask tapMask = ~0;

        [Tooltip("Minimum seconds between reactions so spamming taps doesn't restart the clip.")]
        [SerializeField] private float cooldown = 0.5f;

        [SerializeField] private float rayMaxDistance = 100f;

        [Tooltip("Random seconds between idle expressions (Dig / Smell / Scratch / Yawn). X = min, Y = max.")]
        [SerializeField] private Vector2 idleIntervalRange = new Vector2(10f, 25f);

        [Tooltip("Panels that gate the reaction. The wolf only reacts while at least one of these is " +
                 "active (start / game-over / pause). Leave empty to always allow.")]
        [SerializeField] private GameObject[] requiredPanels;

        private IExpressionAnimator expression;
        private Collider tapCollider;
        private float nextReactTime;
        private float nextIdleTime;

        private void Awake()
        {
            expression = expressionSource as IExpressionAnimator;
            tapCollider = GetComponent<Collider>();
            if (!tapCamera) tapCamera = Camera.main;
            if (expression == null)
                Debug.LogWarning($"{name}: expressionSource doesn't implement IExpressionAnimator.", this);
        }

        private void OnEnable()
        {
            ScheduleNextIdle();
        }

        /// <summary>Pushes the next idle out by a fresh random 10–25s (configurable).</summary>
        private void ScheduleNextIdle()
        {
            nextIdleTime = Time.unscaledTime + Random.Range(idleIntervalRange.x, idleIntervalRange.y);
        }

        private void Update()
        {
            UpdateWolfVisibility();

            if (expression == null) return;
            if (!AnyRequiredPanelActive()) return;

            UpdateRandomIdle();

            if (!tapCamera) return;
            if (Time.unscaledTime < nextReactTime) return;
            if (!TryGetTapPosition(out var screenPos)) return;

            var ray = tapCamera.ScreenPointToRay(screenPos);
            if (!Physics.Raycast(ray, out var hit, rayMaxDistance, tapMask, QueryTriggerInteraction.Collide))
            {
                return;
            }
            if (hit.collider != tapCollider)
            {
                return;
            }
            nextReactTime = Time.unscaledTime + cooldown;
            PlayRandomTapReaction();
        }

        /// <summary>Every <see cref="idleInterval"/> seconds, play a random idle expression.</summary>
        private void UpdateRandomIdle()
        {
            if (Time.unscaledTime < nextIdleTime) return;
            ScheduleNextIdle();
            PlayRandomIdle();
        }

        /// <summary>Picks one of Dig / Smell / Scratch / Yawn.</summary>
        private void PlayRandomIdle()
        {
            // Make sure the wolf isn't lying down / asleep before doing an idle move.
            expression.Lie(false);
            expression.Sleep(false);

            switch (Random.Range(0, 4))
            {
                case 0: expression.Dig();     break;
                case 1: expression.Smell();   break;
                case 2: expression.Scratch(); break;
                default: expression.Yawn();   break;
            }
        }

        /// <summary>Picks one of Bark / Howl / Sleep / Lie on tap.</summary>
        private void PlayRandomTapReaction()
        {
            // A tap always ends a resting state; the chosen case re-applies one if picked.
            expression.Lie(false);
            expression.Sleep(false);

            switch (Random.Range(0, 4))
            {
                case 0:
                    expression.Bark();
                    break;
                case 1:
                    expression.Howl();
                    break;
                case 2: // Sleep
                    expression.Lie(false);
                    expression.Sleep(true);
                    ScheduleNextIdle(); // hold the rest for a fresh random interval
                    break;
                default: // Lie
                    expression.Sleep(false);
                    expression.Lie(true);
                    ScheduleNextIdle(); // hold the rest for a fresh random interval
                    break;
            }
        }

        /// <summary>Show the wolf GameObject while any panel is active, hide it when all are inactive.</summary>
        private void UpdateWolfVisibility()
        {
            if (!WolfGO) return;
            bool shouldShow = AnyRequiredPanelActive();
            if (WolfGO.activeSelf != shouldShow) WolfGO.SetActive(shouldShow);
        }

        /// <summary>
        /// True if no panels are configured (always allow) or at least one configured panel is active.
        /// </summary>
        private bool AnyRequiredPanelActive()
        {
            if (requiredPanels == null || requiredPanels.Length == 0) return true;
            foreach (var panel in requiredPanels)
                if (panel && panel.activeInHierarchy) return true;
            return false;
        }

        /// <summary>True on the frame a tap/click begins, with its screen position.</summary>
        private static bool TryGetTapPosition(out Vector2 screenPos)
        {
            screenPos = default;

            var touch = Touchscreen.current;
            if (touch != null && touch.primaryTouch.press.wasPressedThisFrame)
            {
                screenPos = touch.primaryTouch.position.ReadValue();
                return true;
            }

            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                screenPos = mouse.position.ReadValue();
                return true;
            }

            return false;
        }
    }
}
